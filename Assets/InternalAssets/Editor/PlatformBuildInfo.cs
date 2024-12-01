using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Assets.App.Editor
{
    internal class PlatformBuildInfo
    {
        public BuildTarget Target { get; set; }

        public string PlatformName { get; set; }

        public string FileExtension { get; set; }

        public string BuildFolderPath { get; set; }
    }
}
