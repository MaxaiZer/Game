using Assets.App.Scripts.Events;
using Assets.App.Scripts.Network;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Character
{
    internal class HealthController: NetworkBehaviour
    {
        [SerializeField]
        PlayerState _playerState;
        [SerializeField]
        private int _health;

        private int _maxHealth;

        private void Awake()
        {
            _maxHealth = _health;
        }

        private void OnEnable()
        {
            _playerState.PlayerRespawned += OnRespawn;
        }

        private void OnDisable()
        {
            _playerState.PlayerRespawned -= OnRespawn;
        }

        public void OnRespawn(RespawnEvent e)
        {
            if (!base.IsOwner) return;
            RpcRestoreHealth();

            var healthEvent = new PlayerHealthChangedEvent
            {
                maxHealth = _maxHealth,
                newHealth = _maxHealth
            };
            GlobalBus.Send(healthEvent);
        }

        [Server]
        public void ServerTakeDamage(int playerId, int damage)
        {
            if (_health <= 0 || damage <= 0) return;

            _health -= damage;

            if (_health < 0)
            {
                _health = 0;
            }

            if (_health > 0)
                RpcTakeDamage(Owner, _health);
            else
                PlayersManager.Instance.HandleKill(OwnerId, playerId);
        }

        [ServerRpc(RequireOwnership=false)]
        public void RpcTakeDamage(int playerId, int damage)
        {
            if (_health <= 0 || damage <= 0) return;

            _health -= damage;

            if (_health < 0)
            {
                _health = 0;
            }

            if (_health > 0)
                RpcTakeDamage(Owner, _health);
            else
                PlayersManager.Instance.HandleKill(OwnerId, playerId);              
        }

        [TargetRpc]
        private void RpcTakeDamage(NetworkConnection conn, int newHealth)
        {
            _health = newHealth;

            var e = new PlayerHealthChangedEvent
            {
                maxHealth = _maxHealth,
                newHealth = newHealth
            };

            GlobalBus.Send(e);
        }

        [ServerRpc]
        private void RpcRestoreHealth()
        {
            _health = _maxHealth;
        }
    }
}
