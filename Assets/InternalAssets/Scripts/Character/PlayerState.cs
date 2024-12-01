using System;
using Assets.App.Scripts.Events;
using FishNet.Object;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class PlayerState: NetworkBehaviour
    {
        public event Action PlayerDead;
        public event Action<RespawnEvent> PlayerRespawned;
        public bool IsDead { get; private set; }

        [Server]
        public void ReportAboutDeath()
        {
            ReportObserversAboutDeath();
        }

        [ObserversRpc]
        private void ReportObserversAboutDeath()
        {
            if (IsDead) return;

            IsDead = true;
            PlayerDead?.Invoke();
        }

        [Server]
        public void ReportAboutRespawn(Vector3 pos)
        {
            ReportObserversAboutRespawn(pos);
        }

        [ObserversRpc]
        private void ReportObserversAboutRespawn(Vector3 pos)
        {
            if (!IsDead) return;

            IsDead = false;
            PlayerRespawned?.Invoke(new RespawnEvent { newPosition = pos });
        }
    }
}
