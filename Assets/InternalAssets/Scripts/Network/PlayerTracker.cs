using FishNet.Object;
using MasterServerToolkit.MasterServer;

namespace Assets.App.Scripts.Network
{
    internal class PlayerTracker: NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner) return;

            PlayersManager.Instance.RpcAddPlayer
                (this.NetworkObject, Mst.Client.Auth.AccountInfo.Username);
        }

        /*
         *  would't call on disconnect
         * 
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (!base.IsOwner) return;

            PlayersManager.Instance.RpcRemovePlayer(base.OwnerId);
        }
        */
    }
}
