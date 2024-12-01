using Assets.App.Scripts.Input;
using Assets.App.Scripts.Network;
using Assets.App.Scripts.State;
using MasterServerToolkit.MasterServer;
using MasterServerToolkit.UI;
using System.Linq;
using UnityEngine;

namespace Assets.App.Scripts.UI
{
    public class PlayersListView : UIView
    {
        #region INSPECTOR

        [Header("Components"), SerializeField]
        private UILable uiLablePrefab;
        [SerializeField]
        private UILable uiColLablePrefab;
        [SerializeField]
        private RectTransform listContainer;

        #endregion

        private PlayerInputActions _actions;

        protected override void Awake()
        {
            base.Awake();
            _actions = new();
        }

        private void OnEnable() => _actions.Enable();

        private void OnDisable() => _actions.Disable();

        protected override void Start()
        {
            base.Start();

            if (listContainer)
            {
                foreach (Transform t in listContainer)
                {
                    Destroy(t.gameObject);
                }
            }
        }

        private void Update()
        {
            if (_actions.Menu.ShowPlayersList.IsPressed() && 
                GameStateManager.Instance.CurrentState != GameState.Paused)
            {
                if (!this.isVisible)
                    Show();
            }
            else if (this.isVisible)
                Hide();    
        }

        protected override void OnShow()
        {
            ClearPlayersList();
            DrawPlayersList();
        }

        private void DrawPlayersList()
        {
            if (listContainer)
            {
                int index = 0;

                Instantiate(uiColLablePrefab, listContainer, false).Text = "#";
                Instantiate(uiColLablePrefab, listContainer, false).Text = "Name";
                Instantiate(uiColLablePrefab, listContainer, false).Text = "Kills";
                Instantiate(uiColLablePrefab, listContainer, false).Text = "Deaths";

                foreach (PlayerInfo info in PlayersManager.Instance.GetPlayersInfo())
                {
                    var playerIndoexLable = Instantiate(uiLablePrefab, listContainer, false);
                    playerIndoexLable.Text = (index + 1).ToString();
                    playerIndoexLable.name = $"playerIndoexLable_{index}";

                    var playerNameLable = Instantiate(uiLablePrefab, listContainer, false);
                    playerNameLable.Text = info.name;
                    playerNameLable.name = $"playerNameLable_{index}";

                    var playerKillsLable = Instantiate(uiLablePrefab, listContainer, false);
                    playerKillsLable.Text = info.kills.ToString();
                    playerKillsLable.name = $"playerKillsLable_{index}";

                    var playerDeathsLable = Instantiate(uiLablePrefab, listContainer, false);
                    playerDeathsLable.Text = info.deaths.ToString();
                    playerDeathsLable.name = $"playerDeathsLable_{index}";

                    index++;
                }
            }
            else
            {
                logger.Error("Not all components are setup");
            }
        }

        private void ClearPlayersList()
        {
            if (listContainer)
            {
                foreach (Transform tr in listContainer)
                {
                    Destroy(tr.gameObject);
                }
            }
        }
    }
}