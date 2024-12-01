using FishNet.Object;
using MasterServerToolkit.MasterServer;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class PlayerName: NetworkBehaviour
    {
        [SerializeField]
        private TextMeshPro _text;

        private float _transparentMinDistance = 40;
        private float _visibleMaxDistance = 10f;
        private float _transparencyUpdateTime = 0.3f;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (base.IsOwner)
            {
                RpcSetName(Mst.Client.Auth.AccountInfo.Username);
                _text.enabled = false;
                this.enabled = false;
            } else
            {
                StartCoroutine(TextTransparencyUpdator());
            }
        }

        private void LateUpdate()
        {
            _text.transform.rotation = Quaternion.LookRotation((_text.transform.position - Camera.main.transform.position).normalized);
        }

        [ServerRpc]
        private void RpcSetName(string name)
        {
            RpcObserversSetName(name);
        }

        [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
        private void RpcObserversSetName(string name)
        {
            _text.text = name;
        }

        private IEnumerator TextTransparencyUpdator()
        {
            while (true)
            {
                float distance = Vector3.Distance(Camera.main.transform.position, _text.transform.position);
                float transparency = Mathf.Clamp01((distance - _transparentMinDistance) / (_visibleMaxDistance - _transparentMinDistance));
                _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, transparency);

                yield return new WaitForSeconds(_transparencyUpdateTime);
            }
        }
    }
}
