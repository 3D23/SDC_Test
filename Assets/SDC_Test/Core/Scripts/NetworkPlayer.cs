using Mirror;
using SDC_Test.Core.Installers;
using SDC_Test.NM.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SDC_Test.Core
{
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [Inject] private INMClient nmClient;

        private bool mIsUnsubscribed = true;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!isLocalPlayer)
                return;

            if (nmClient == null)
            {
                var sceneScope = LifetimeScope.Find<NetworkLifetimeScope>();
                if (sceneScope != null)
                    sceneScope.Container.Inject(this);
            }

            if (nmClient == null)
            {
                Debug.LogError($"[{nameof(NetworkPlayer)}] {nameof(INMClient)} is null!");
                return;
            }

            mIsUnsubscribed = false;

            nmClient.SubscribeClient<HelloMessage>(OnHelloMessageReceived);

#if DEBUG
            Debug.Log($"[{nameof(NetworkPlayer)}] Local player subscribed to HelloMessage.");
#endif
        }

        private void OnHelloMessageReceived(in HelloMessage message)
        {
#if DEBUG
            Debug.Log($"<color=#32cd32>[{nameof(NetworkPlayer)}, CLIENT RECEIVE]</color>" +
                $" Message processed by {gameObject.name}: <color=#ffffff>\"{message.Message}\"</color>");
#endif
        }

        public override void OnStopClient()
        {
            UnsubscribeIfNeeded();
            base.OnStopClient();
        }

        private void OnDestroy()
        {
            UnsubscribeIfNeeded();
        }

        private void UnsubscribeIfNeeded()
        {
            if (mIsUnsubscribed || !isLocalPlayer)
                return;

            mIsUnsubscribed = true;
            nmClient?.UnsubscribeClient<HelloMessage>(OnHelloMessageReceived);

#if DEBUG
            Debug.Log($"[{nameof(NetworkPlayer)}] Unsubscribed from HelloMessage.");
#endif
        }
    }
}
