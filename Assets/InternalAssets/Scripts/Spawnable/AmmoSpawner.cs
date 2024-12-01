using FishNet;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class AmmoSpawner: NetworkBehaviour
    {
        [SerializeField]
        private GameObject _ammoPrefab;
        [SerializeField]
        private Transform[] _spawns;
        [SerializeField]
        private int _targetAmmoOnMap = 2;

        private int _ammoOnMap = 0;
        private List<(NetworkObject, int)> _spawnedObjectsWithSpawnIdx = new();
        private List<int> _recentlyUsedSpawnIndices = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            while (_ammoOnMap < _targetAmmoOnMap)
                Spawn();
        }

        private void Spawn()
        {
            if (_ammoOnMap == _targetAmmoOnMap) return;

            int spawnPointIndex = GetRandomUnusedSpawnPointIndex();
            if (spawnPointIndex < 0) return;

            NetworkObject obj = InstanceFinder.NetworkManager.GetPooledInstantiated(_ammoPrefab, false);
            obj.transform.position = _spawns[spawnPointIndex].position;
            _spawnedObjectsWithSpawnIdx.Add((obj, spawnPointIndex));

            InstanceFinder.ServerManager.Spawn(obj, null);
            obj.GetComponent<AmmoBox>().PlayerInteracted += Despawn;

            _ammoOnMap++;
        }

        private int GetRandomUnusedSpawnPointIndex()
        {
            List<int> unusedSpawnPointIndices = new List<int>();
            for (int i = 0; i < _spawns.Length; i++)
            {
                if (!_recentlyUsedSpawnIndices.Contains(i))
                {
                    unusedSpawnPointIndices.Add(i);
                }
            }

            if (unusedSpawnPointIndices.Count == 0)
            {
                return -1;
            }

            int randomIndex = Random.Range(0, unusedSpawnPointIndices.Count);
            int selectedSpawnPointIndex = unusedSpawnPointIndices[randomIndex];

            _recentlyUsedSpawnIndices.Add(selectedSpawnPointIndex);

            if (_recentlyUsedSpawnIndices.Count > _targetAmmoOnMap)
            {
                _recentlyUsedSpawnIndices.RemoveAt(0);
            }

            return selectedSpawnPointIndex;
        }

        private void Despawn(AmmoBox obj)
        {
            int spawnPointIndex = _spawnedObjectsWithSpawnIdx.Find(x => x.Item1 == obj).Item2;
            _spawnedObjectsWithSpawnIdx.RemoveAll(x => x.Item1 == obj);
            _recentlyUsedSpawnIndices.Remove(spawnPointIndex);

            InstanceFinder.ServerManager.Despawn(obj.NetworkObject, DespawnType.Pool);
            obj.PlayerInteracted -= Despawn;

            _ammoOnMap--;           
            Spawn();
        }

    }
}
