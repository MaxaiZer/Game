using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class NotOwnerScriptsDisabler: NetworkBehaviour
    {
        [SerializeField]
        List<MonoBehaviour> _scriptsToDisable = new();

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner)
            {
                foreach (var script in _scriptsToDisable)
                {
                    script.enabled = false;
                }
            }
        }
    }
}
