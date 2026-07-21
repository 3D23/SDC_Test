using SDC_Test.NM.Implementation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SDC_Test.Core.Installers
{
    public sealed class NetworkLifetimeScope : LifetimeScope
    {
        [SerializeField] private NetworkManager m_NetworkManager;
        [SerializeField] private NetworkGuiTester m_NetworkGuiTester;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<NMService>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
 
            if (m_NetworkManager != null)
                builder.RegisterComponent(m_NetworkManager);

            if (m_NetworkManager != null 
                && m_NetworkManager.playerPrefab != null)
            {
                if (m_NetworkManager.playerPrefab.TryGetComponent<NetworkPlayer>(out var playerComponent))
                    builder.RegisterComponentInNewPrefab(playerComponent, Lifetime.Transient);
#if DEBUG
                else
                    Debug.LogError($"[{nameof(NetworkLifetimeScope)}] The playerPrefab in NetworkManager does not contain the {nameof(NetworkPlayer)} component!");
#endif
            }

            if (m_NetworkGuiTester != null)
                builder.RegisterComponent(m_NetworkGuiTester);
        }
    }
}