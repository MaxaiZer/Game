using Assets.App.Scripts.Events;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEventBus;

namespace Assets.App.Scripts.UI
{
    internal class HudManager: MonoBehaviour,      
        IListener<PlayerHealthChangedEvent>,
        IListener<SelectedGunAmmoChangedEvent>
    {

        [SerializeField]
        private TextMeshProUGUI _healthText;
        [SerializeField]
        private TextMeshProUGUI _ammoText;
        [SerializeField]
        private Image _blood;

        private float _bloodMaxAlpha = 0.35f;
        Coroutine ChangeBloodAplhaCoroutine = null;

        private void OnEnable()
        {
            GlobalBus.Subscribe(this);
        }

        private void OnDisable()
        {
            GlobalBus.UnSubscribe(this);
        }

        public void React(in PlayerHealthChangedEvent e)
        {
            _healthText.text = e.newHealth.ToString();

            if (ChangeBloodAplhaCoroutine != null)
            {
                StopCoroutine(ChangeBloodAplhaCoroutine);
            }

            float targetAlpha = (1 - ((float)e.newHealth / (float)e.maxHealth)) * _bloodMaxAlpha;
            ChangeBloodAplhaCoroutine = StartCoroutine(ChangeBloodAlpha(targetAlpha));
        }

        public void React(in SelectedGunAmmoChangedEvent e)
        {
            _ammoText.text = e.magazineAmmoLeft.ToString() + "/" + e.ammoTotal.ToString();
        }

        private IEnumerator ChangeBloodAlpha(float targetAlpha)
        {
            float time = 0;
            float duration = 0.1f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float a = Mathf.Lerp(_blood.color.a, targetAlpha, time / duration);
                _blood.color = new Color(1, 1, 1, a);
                yield return null;
            }

            _blood.color = new Color(1, 1, 1, targetAlpha);
        }
    }
}
