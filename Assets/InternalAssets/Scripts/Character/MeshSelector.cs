using FishNet.Component.Spawning;
using FishNet.Object;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    [RequireComponent(typeof(MeshFilter))]
    internal class MeshSelector: NetworkBehaviour
    {
        [SerializeField]
        Mesh[] _meshes = new Mesh[0];

        MeshFilter _meshFilter;

        private void Awake()
        {
            _meshFilter = this.GetComponent<MeshFilter>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner) return;

            if (_meshes.Length == 0)
            {
                Debug.LogError("Empty meshes");
                return;
            }

            int selectedMeshIdx = Random.Range(0, _meshes.Length);
            _meshFilter.mesh = _meshes[selectedMeshIdx];

            RpcSelectMesh(selectedMeshIdx);
        }

        [ServerRpc]
        private void RpcSelectMesh(int idx)
        {
            ObserversSelectMesh(idx);
        }

        [ObserversRpc(BufferLast = true, ExcludeOwner = true)]
        private void ObserversSelectMesh(int idx)
        {
            _meshFilter.mesh = _meshes[idx];
        }

    }
}
