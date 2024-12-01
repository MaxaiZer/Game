/*using Assets.App.Scripts.Events;
using FishNet.Object;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Character
{
    [RequireComponent(typeof(RenderersEnabler))]
    internal class PlayerEnabler: NetworkBehaviour
    {
        private RenderersEnabler _renderersEnabler;

        private void Awake()
        {
            _renderersEnabler = this.GetComponent<RenderersEnabler>();
        }

        [Server]
        public void DisablePlayerOnDeath()
        {
            RpcDisablePlayer();
        }

        [ObserversRpc]
        private void RpcDisablePlayer()
        {
            _renderersEnabler.Disable();
            if (!base.IsOwner) return;

            GlobalBus.Send(new DeathEvent());
        }

        [Server]
        public void RespawnPlayer(Vector3 pos)
        {
            RpcRespawnPlayer(pos);
        }

        [ObserversRpc]
        private void RpcRespawnPlayer(Vector3 pos)
        {
            _renderersEnabler.Enable();
            if (!base.IsOwner) return;

            GlobalBus.Send(new RespawnEvent { newPosition = pos});
        }

    }
}
*/