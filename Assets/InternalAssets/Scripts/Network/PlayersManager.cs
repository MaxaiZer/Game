using Assets.App.Scripts.Character;
using FishNet;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.App.Scripts.Network
{
    internal class PlayersManager: NetworkBehaviour
    {
        public PlayerSpawner playerSpawner;

        public static PlayersManager Instance { get; private set; }

        [SerializeField]
        private float _respawnTime = 2f;

        [SyncObject]
        private readonly SyncList<PlayerInfo> _playerInfos = new SyncList<PlayerInfo>();

        private List<NetworkObject> _players = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnDisable()
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        [Server]
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
           // if (args.ConnectionState == RemoteConnectionState.Started)

            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                Debug.Log("Removed " + conn.ClientId);
                _playerInfos.Remove(_playerInfos.Find(p => p.id == conn.ClientId));
                _players.Remove(_players.Find(p => p.OwnerId == conn.ClientId));
            }              
        }

        public List<PlayerInfo> GetPlayersInfo()
        {
            return _playerInfos.ToList();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcAddPlayer(NetworkObject obj, string name)
        {
            Debug.Log("added " + obj.OwnerId);
            _playerInfos.Add(new PlayerInfo(obj.OwnerId, name));
            _players.Add(obj);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcRemovePlayer(int id)
        {
            Debug.Log("Removed " + id);
            _playerInfos.Remove(_playerInfos.Find(p => p.id == id));
            _players.Remove(_players.Find(p => p.OwnerId == id));
        }

        [Server]
        public void HandleKill(int victimId, int killerId)
        {
            int killerIdx = _playerInfos.FindIndex(p => p.id == killerId);
            if (killerIdx < 0) return;

            _playerInfos[killerIdx].kills++;
            _playerInfos.Dirty(killerIdx);

            HandleDeath(victimId);
        }

        [Server]
        public void HandleDeath(int playerId)
        {
            int playerIdx = _playerInfos.FindIndex(p => p.id == playerId);
            if (playerIdx < 0) return;

            _playerInfos[playerIdx].deaths++;
            _playerInfos.Dirty(playerIdx);

            StartCoroutine(PlayerRespawn(_players.Find(p => p.OwnerId == playerId)));
        }

        [Server]
        private IEnumerator PlayerRespawn(NetworkObject player)
        {
            var state = player.GetComponent<PlayerState>();
            state.ReportAboutDeath();

            yield return new WaitForSeconds(_respawnTime);
            Vector3 spawnPos = playerSpawner.Spawns[Random.Range(0, playerSpawner.Spawns.Length)].position;
            state.ReportAboutRespawn(spawnPos);
        }
    }
}
