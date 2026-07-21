using Mirror;
using SDC_Test.NM.Contracts;
using SDC_Test.NM.InternalMessages;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDC_Test.NM.Implementation
{
    public sealed class NMService :
        INMClient,
        INMServer,
        INMLifecycle,
        IDisposable
    {
        private readonly Dictionary<Type, NMHandlerCollection> mClientHandlers = new();
        private readonly Dictionary<Type, HashSet<NetworkConnectionToClient>> mServerClientSubscriptions = new();
        private readonly Dictionary<Type, ushort> mCacheTypeToId = new();
        private readonly Dictionary<ushort, Type> mCacheIdToType = new();

        private bool mIsServerInitialized;
        private bool mIsClientInitialized;
        private bool mIsDisposed;

        #region Lifetime API

        public void InitializeServer()
        {
            if (mIsDisposed)
                return;

            if (mIsServerInitialized)
            {
                NetworkServer.UnregisterHandler<ClientSubscribeMessage>();
                NetworkServer.UnregisterHandler<ClientUnsubscribeMessage>();
            }

            NetworkServer.RegisterHandler<ClientSubscribeMessage>(OnClientSubscribeRequested);
            NetworkServer.RegisterHandler<ClientUnsubscribeMessage>(OnClientUnsubscribeRequested);

            if (!mIsServerInitialized)
            {
                WarmUpTypeCache();
                mIsServerInitialized = true;

#if DEBUG
                Debug.Log($"[{nameof(NMService)}] Server Architecture Initialized!");
#endif
            }
        }

        public void InitializeClient()
        {
            if (mIsDisposed)
                return;

            if (mIsClientInitialized)
            {
                NetworkClient.UnregisterHandler<NetworkMessageWrapper>();
            }

            NetworkClient.RegisterHandler<NetworkMessageWrapper>(OnReceiveWrapperOnClient);

            if (!mIsClientInitialized)
            {
                WarmUpTypeCache();
                mIsClientInitialized = true;
#if DEBUG
                Debug.Log($"[{nameof(NMService)}] Client Architecture Initialized!");
#endif
            }
        }

        public void ClearClientSubscriptionOnDisconnect(NetworkConnectionToClient conn)
        {
            if (mIsDisposed || 
                conn == null || 
                !mIsServerInitialized)
                return;

            foreach (var subscribers in mServerClientSubscriptions.Values)
                subscribers.Remove(conn);
        }

        #endregion

        #region Client API

        public void SubscribeClient<T>(NMAction<T> handler)
            where T : struct, NetworkMessage
        {
            if (mIsDisposed)
                return;

            if (!mIsClientInitialized)
            {
#if DEBUG
                Debug.LogWarning($"[{nameof(NMService)}] Cannot subscribe to {typeof(T).Name}. " +
                    $"The client lifecycle is not initialized. Call InitializeClient first.");
#endif
                return;
            }

            if (handler == null)
                return;

            Type messageType = typeof(T);
            ushort msgId = GetMessageId(messageType);

            if (!mClientHandlers.TryGetValue(messageType, out var handlersCollection))
            {
                handlersCollection = new NMHandlerCollection();
                mClientHandlers[messageType] = handlersCollection;
            }

            void readerWrapper(NetworkReader reader)
            {
                T message = reader.Read<T>();
                handler.Invoke(in message);
            }

            if (!handlersCollection.TryAdd(handler, readerWrapper))
            {
#if DEBUG
                Debug.LogWarning($"[{nameof(NMService)}] Failed to subscribe to {typeof(T).Name}. " +
                    $"Handler is either null or already subscribed.");
#endif
                return;
            }

            if (handlersCollection.Count == 1 && NetworkClient.isConnected)
                NetworkClient.Send(new ClientSubscribeMessage(messageTypeId: msgId));
        }

        public void UnsubscribeClient<T>(NMAction<T> handler)
            where T : struct, NetworkMessage
        {
            if (mIsDisposed)
                return;

            if (!mIsClientInitialized)
            {
#if DEBUG
                Debug.LogWarning($"[{nameof(NMService)}] Cannot unsubscribe from {typeof(T).Name}. " +
                    $"The client is not initialized.");
#endif
                return;
            }

            if (handler == null)
                return;

            Type messageType = typeof(T);
            if (!mClientHandlers.TryGetValue(messageType, out var handlersCollection))
                return;

            if (!handlersCollection.Remove(handler))
                return;

            if (handlersCollection.Count == 0)
            {
                ushort msgId = GetMessageId(messageType);

                handlersCollection.Clear();
                mClientHandlers.Remove(messageType);

                if (NetworkClient.isConnected)
                    NetworkClient.Send(new ClientUnsubscribeMessage(messageTypeId: msgId));
            }
        }

        #endregion

        #region Server API

        public void Raise<T>(in T message)
            where T : struct, NetworkMessage
        {
            if (mIsDisposed)
                return;

            if (!mIsServerInitialized)
            {
#if DEBUG
                Debug.LogWarning($"[{nameof(NMService)}] Cannot raise network event for {typeof(T).Name}. " +
                    $"The server lifecycle is not initialized. Call InitializeServer first.");
#endif
                return;
            }

            Type messageType = typeof(T);
            if (!mServerClientSubscriptions.TryGetValue(messageType, out var subscribers) || subscribers.Count == 0)
            {
#if DEBUG
                Debug.Log($"<color=#ff00ff>No subscribed clients!</color>");
#endif
                return;
            }

            ushort msgId = GetMessageId(messageType);

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(message);
            var payloadBytes = writer.ToArray();

            NetworkMessageWrapper wrapper = new(messageTypeId: msgId, payload: payloadBytes);

#if DEBUG
            Debug.Log($"<color=#00ffff>[{nameof(NMService)}, SERVER RAISE]</color> " +
                $"Sending <color=#fff200>{messageType.Name}</color> to " +
                $"<color=#ff00ff>{subscribers.Count}</color> subscribed client(s).");
#endif

            foreach (var conn in subscribers)
            {
                if (conn.isReady)
                    conn.Send(wrapper);
            }
        }

        #endregion

        #region VContainer Lifetime

        public void Dispose()
        {
            if (mIsDisposed)
                return;

            mIsDisposed = true;

            try
            {
                if (mIsServerInitialized)
                {
                    NetworkServer.UnregisterHandler<ClientSubscribeMessage>();
                    NetworkServer.UnregisterHandler<ClientUnsubscribeMessage>();
                }

                if (mIsClientInitialized)
                {
                    NetworkClient.UnregisterHandler<NetworkMessageWrapper>();
                    UnsubscribeAll();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.LogError($"[{nameof(NMService)}] Error during disposal: {ex.Message}");
#endif
            }
            finally
            {
                foreach (var collection in mClientHandlers.Values)
                    collection.Clear();

                mClientHandlers.Clear();
                mServerClientSubscriptions.Clear();
                mCacheTypeToId.Clear();
                mCacheIdToType.Clear();

                mIsServerInitialized = false;
                mIsClientInitialized = false;

#if DEBUG
                Debug.Log($"[{nameof(NMService)}] Service Disposed!");
#endif
            }
        }

        #endregion

        #region Private Methods

        private void WarmUpTypeCache()
        {
            if (mCacheTypeToId.Count > 0)
                return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(NetworkMessage).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            ushort msgId = CalculateHash(type);

                            if (mCacheIdToType.ContainsKey(msgId))
                            {
                                Debug.LogError($"[{nameof(NMService)}] Hash collision detected! " +
                                    $"{type.Name} and {mCacheIdToType[msgId].Name} have same ID: {msgId}");
                                continue;
                            }

                            mCacheTypeToId[type] = msgId;
                            mCacheIdToType[msgId] = type;
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.LogError($"[{nameof(NMService)}] Error processing: {ex.Message}");
#endif
                }
            }
        }

        private static ushort CalculateHash(Type type)
        {
            string fullName = type.FullName ?? type.Name;
            unchecked
            {
                int hash = 23;
                foreach (char c in fullName)
                    hash = hash * 31 + c;

                ushort result = (ushort)(hash & 0xFFFF);

                return result == 0
                    ? (ushort)1
                    : result;
            }
        }

        private ushort GetMessageId(Type type)
        {
            if (mCacheTypeToId.TryGetValue(type, out ushort id))
                return id;

            throw new KeyNotFoundException($"[{nameof(NMService)}] Type {type.Name} was not found in the network message cache. " +
                                   $"Ensure that it implements the Mirror.NetworkMessage interface.");
        }

        private void OnClientSubscribeRequested(NetworkConnectionToClient conn, ClientSubscribeMessage msg)
        {
            if (conn == null)
                return;

            if (mCacheIdToType.TryGetValue(msg.MessageTypeId, out Type messageType))
            {
                if (!mServerClientSubscriptions.ContainsKey(messageType))
                    mServerClientSubscriptions[messageType] = new HashSet<NetworkConnectionToClient>();

                mServerClientSubscriptions[messageType].Add(conn);
            }
        }

        private void OnClientUnsubscribeRequested(NetworkConnectionToClient conn, ClientUnsubscribeMessage msg)
        {
            if (conn == null)
                return;

            if (mCacheIdToType.TryGetValue(msg.MessageTypeId, out Type messageType))
            {
                if (mServerClientSubscriptions.TryGetValue(messageType, out var subscribers))
                {
                    subscribers.Remove(conn);
                    if (subscribers.Count == 0)
                        mServerClientSubscriptions.Remove(messageType);
                }
            }
        }

        private void OnReceiveWrapperOnClient(NetworkMessageWrapper wrapper)
        {
            if (mCacheIdToType.TryGetValue(wrapper.MessageTypeId, out Type messageType))
            {
                if (mClientHandlers.TryGetValue(messageType, out var handlersCollection))
                {
                    NetworkReaderPooled reader = NetworkReaderPool.Get(wrapper.Payload);
                    try
                    {
                        foreach (var (Original, Wrapper) in handlersCollection)
                        {
                            reader.Position = 0;
                            Wrapper.Invoke(reader);
                        }
                    }
                    finally
                    {
                        NetworkReaderPool.Return(reader);
                    }
                }
            }
        }

        private void UnsubscribeAll()
        {
            if (mIsDisposed)
                return;

            if (!mIsClientInitialized)
            {
#if DEBUG
                Debug.LogWarning($"[{nameof(NMService)}] Cannot unsubscribe all handlers. " +
                    $"The client lifecycle is not initialized.");
#endif
                return;
            }

            if (NetworkClient.isConnected)
            {
                foreach (Type messageType in mClientHandlers.Keys)
                {
                    ushort msgId = GetMessageId(messageType);
                    NetworkClient.Send(new ClientUnsubscribeMessage(messageTypeId: msgId));
                }
            }

            foreach (var collection in mClientHandlers.Values)
                collection.Clear();

            mClientHandlers.Clear();
        }

        #endregion
    }
}