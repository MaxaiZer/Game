using FishNet;
using FishNet.Object;
using GameKit.Utilities.ObjectPooling;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class BloodSpawner: NetworkBehaviour
    {
        [SerializeField]
        private GameObject _bloodParticle;
        [SerializeField]
        private float _lifeTime = 0.3f;

        [Server]
        public void SpawnForAllPlayers(Vector3 lookAtPos, Vector3 pos)
        {
            ObserversSpawn(lookAtPos, pos);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcSpawn(Vector3 lookAtPos, Vector3 pos)
        {
            //Spawn(lookAtPos, pos, _lifeTime);
            ObserversSpawn(lookAtPos, pos);
        }

        [ObserversRpc]
        private void ObserversSpawn(Vector3 lookAtPos, Vector3 pos)
        {
            GameObject blood = ObjectPool.Retrieve(_bloodParticle);
            blood.SetActive(true);
            blood.transform.position = pos;
            blood.transform.parent = this.transform;
            blood.transform.LookAt(lookAtPos);
            StartCoroutine(DespawnParticle(blood, _lifeTime));
        }

        [Server]
        private void Spawn(Vector3 lookAtPos, Vector3 pos, float lifeTime)
        {
           // InstanceFinder.NetworkManager.ObjectPool
            NetworkObject obj = InstanceFinder.NetworkManager.GetPooledInstantiated(_bloodParticle, false);
            obj.transform.LookAt(lookAtPos);
            obj.SetParent(this);
           // obj.transform.parent = this.transform;
            obj.transform.position = pos;

            InstanceFinder.ServerManager.Spawn(obj, null);
            StartCoroutine(DespawnParticle(obj, lifeTime));
        }

        [Server]
        private IEnumerator DespawnParticle(NetworkObject obj, float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);
            //obj.transform.parent = null;
            obj.SetParent((NetworkObject)null);
            InstanceFinder.ServerManager.Despawn(obj, DespawnType.Pool);
        }

        private IEnumerator DespawnParticle(GameObject obj, float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);
            ObjectPool.Store(obj);
        }
    }
}
