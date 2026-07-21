using Mirror;
using SDC_Test.NM.Contracts;
using VContainer;

namespace SDC_Test.Core
{
    public sealed class NetworkManager : Mirror.NetworkManager
    {
        private INMLifecycle mService;

        [Inject]
        public void Construct(INMLifecycle servicelifecycle)
        {
            mService = servicelifecycle;
        }


        public override void OnStartServer()
        {
            base.OnStartServer();
            mService?.InitializeServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            mService?.InitializeClient();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            mService?.ClearClientSubscriptionOnDisconnect(conn);
            base.OnServerDisconnect(conn);
        }
    }
}
