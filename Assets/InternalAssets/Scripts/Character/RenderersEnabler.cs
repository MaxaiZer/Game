using Assets.App.Scripts.Events;
using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class RenderersEnabler: NetworkBehaviour
    {
        [SerializeField]
        PlayerState _playerState;
        [SerializeField]
        List<Renderer> _renderersDisableForOwner = new();
        [SerializeField]
        List<Renderer> _ignoredRenderers = new();

        List<Renderer> _renderers;

        private void Awake()
        {
            _renderers = this.GetComponentsInChildren<Renderer>().ToList();
            _renderers.RemoveAll(r => _ignoredRenderers.Contains(r));
        }

        private void OnEnable()
        {
            _playerState.PlayerDead += OnDeath;
            _playerState.PlayerRespawned += OnRespawn;
        }

        private void OnDisable()
        {
            _playerState.PlayerDead -= OnDeath;
            _playerState.PlayerRespawned -= OnRespawn;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner) return;

            foreach (var render in _renderersDisableForOwner)
            {
                render.enabled = false;
            }
        }

        private void OnDeath() => Disable();

        private void OnRespawn(RespawnEvent e) => Enable();

        private void Enable()
        {
            if (base.IsOwner)
            {
                EnableForOwner();
            }
            else
            {
                EnableForObservers();
            }
        }

        private void Disable()
        {
            foreach (var render in _renderers)
            {
                render.enabled = false;
            }
        }

        private void EnableForOwner()
        {
            foreach (var render in _renderers)
            {
                if (!_renderersDisableForOwner.Contains(render))
                    render.enabled = true;
            }
        }

        private void EnableForObservers()
        {
            foreach (var render in _renderers)
            {
                render.enabled = true;
            }
        }

    }
}
