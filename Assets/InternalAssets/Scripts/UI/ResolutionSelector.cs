using MasterServerToolkit.UI;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.InternalAssets.Scripts.UI
{
    [System.Serializable]
    internal struct ResolutionInfo
    {
        public int width;
        public int height;
    }

    internal class ResolutionSelector: MonoBehaviour
    {
        [SerializeField]
        UIView _settingsView;
        [SerializeField]
        TextMeshProUGUI _resolutionText;
        [SerializeField]
        private List<ResolutionInfo> _resolutions;
        private int _selectedResolutionIdx = 0;

        private void OnEnable()
        {
            _settingsView.OnShowEvent.AddListener(ShowCurrentResolution);
        }

        private void OnDisable()
        {
            _settingsView.OnShowEvent.RemoveListener(ShowCurrentResolution);
        }

        public void SelectPrevious()
        {
            _selectedResolutionIdx--;
            if (_selectedResolutionIdx == 0)
                _selectedResolutionIdx = _resolutions.Count - 1;

            UpdateResolution();
        }

        public void SelectNext()
        {
            _selectedResolutionIdx = (_selectedResolutionIdx + 1) % _resolutions.Count;

            UpdateResolution();
        }

        private void ShowCurrentResolution()
        {
            int idx = _resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            if (idx >= 0)
                _selectedResolutionIdx = idx;
            else
                _selectedResolutionIdx = 0;

            _resolutionText.text = Screen.width + "x" + Screen.height;
        }

        private void UpdateResolution()
        {
            Screen.SetResolution(_resolutions[_selectedResolutionIdx].width,
                _resolutions[_selectedResolutionIdx].height, Screen.fullScreen);

            _resolutionText.text = _resolutions[_selectedResolutionIdx].width + "x" + 
                _resolutions[_selectedResolutionIdx].height;
        }

    }
}
