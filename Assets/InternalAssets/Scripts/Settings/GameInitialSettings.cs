using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.App.Scripts.Settings
{
    internal class GameInitialSettings: MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            //Screen.fullScreenMode = PlayerSettings.FullScreenMode;
        }
    }
}
