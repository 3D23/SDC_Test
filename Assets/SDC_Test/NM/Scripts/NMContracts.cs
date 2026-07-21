using Mirror;

namespace SDC_Test.NM.Contracts
{
    /// <summary>
    /// Represents a delegate for handling incoming network messages.
    /// Uses the 'in' modifier to pass the message struct by reference without allocation or copying.
    /// </summary>
    public delegate void NMAction<T>(in T message) 
        where T : struct, NetworkMessage;

    /// <summary>
    /// Defines infrastructure methods for managing the underlying network service lifecycle.
    /// Intended to be invoked exclusively by custom implementations of Mirror's NetworkManager.
    /// </summary>
    public interface INMLifecycle
    {
        /// <summary>
        /// Initializes the server-side network architecture, registers baseline infrastructure message handlers, 
        /// and builds the cache of existing network message identifiers.
        /// </summary>
        void InitializeServer();

        /// <summary>
        /// Initializes the client-side network architecture, registers the primary system message wrapper handler, 
        /// and populates the local message identification cache.
        /// </summary>
        void InitializeClient();

        /// <summary>
        /// Clears all existing subscriptions associated with the specified client connection.
        /// Must be called during server disconnection routines to prevent memory leaks.
        /// </summary>
        void ClearClientSubscriptionOnDisconnect(NetworkConnectionToClient conn);
    }

    /// <summary>
    /// Provides client-side operations for managing type-safe event subscriptions and dispatching internal actions.
    /// Restricts application logic from tampering with infrastructure setup or server-exclusive configurations.
    /// </summary>
    public interface INMClient
    {
        /// <summary>
        /// Registers a callback handler for the specified network message type.
        /// Automatically synchronizes subscription status with the server upon successful connection.
        /// </summary>
        void SubscribeClient<T>(NMAction<T> handler) 
            where T : struct, NetworkMessage;

        /// <summary>
        /// Unregisters a previously added callback handler for the specified network message type.
        /// Automatically notifies the server if the local client subscription list for this type becomes empty.
        /// </summary>
        void UnsubscribeClient<T>(NMAction<T> handler) 
            where T : struct, NetworkMessage;
    }

    /// <summary>
    /// Provides server-exclusive functionality for broadcasting events to registered and verified clients.
    /// Ensures isolation by preventing server business logic from accessing client-only subscription handlers.
    /// </summary>
    public interface INMServer
    {
        /// <summary>
        /// Broadcasts the specified network message payload to all clients who have explicitly subscribed to this message type.
        /// Filters out non-subscribed clients to guarantee security and prevent abrupt client disconnections by Mirror.
        /// </summary>
        void Raise<T>(in T message) 
            where T : struct, NetworkMessage;
    }
}
