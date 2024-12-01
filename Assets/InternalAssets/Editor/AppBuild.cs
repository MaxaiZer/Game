#if FISHNET
using MasterServerToolkit.MasterServer;
using MasterServerToolkit.Utils.Editor;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Assets.App.Editor
{
    public class AppBuild
    {
        private const string _toolMenu = "Tools/App Build/";
        private const string _buildFolderName = "Builds";


        private static readonly PlatformBuildInfo _windowsBuildInfo = new ()
        {
            Target = BuildTarget.StandaloneWindows64,
            FileExtension = "exe",
            PlatformName = "Windows",
            BuildFolderPath = Directory.GetCurrentDirectory()
        };

        private static readonly PlatformBuildInfo _linuxBuildInfo = new ()
        {
            Target = BuildTarget.StandaloneLinux64,
            FileExtension = "x86_64",
            PlatformName = "Linux",
            BuildFolderPath = "\\root\\app"
        };

        [MenuItem(_toolMenu + "/Windows/Room(Headless)")]
        private static void BuildRoomForWindowsHeadless()
        {
            BuildRoom(_windowsBuildInfo, true);
        }

        [MenuItem(_toolMenu + "/Windows/Room(Normal)")]
        private static void BuildRoomForWindowsNormal()
        {
            BuildRoom(_windowsBuildInfo, false);
        }

        [MenuItem(_toolMenu + "/Windows/Master Server and Spawner")]
        private static void BuildMasterAndSpawnerForWindows()
        {
            BuildMasterAndSpawner(_windowsBuildInfo);
        }

        [MenuItem(_toolMenu + "/Windows/Spawner")]
        private static void BuildSpawnerForWindows()
        {
            BuildSpawner(_windowsBuildInfo);
        }

        [MenuItem(_toolMenu + "/Windows/Client")]
        private static void BuildClientForWindows()
        {
            BuildClient(_windowsBuildInfo);
        }

        [MenuItem(_toolMenu + "/Linux/Room(Headless)")]
        private static void BuildRoomForLinuxHeadless()
        {
            BuildRoom(_linuxBuildInfo, true);
        }

        [MenuItem(_toolMenu + "/Linux/Room(Normal)")]
        private static void BuildRoomForLinuxNormal()
        {
            BuildRoom(_linuxBuildInfo, false);
        }

        [MenuItem(_toolMenu + "/Linux/Master Server and Spawner")]
        private static void BuildMasterAndSpawnerForLinux()
        {
            BuildMasterAndSpawner(_linuxBuildInfo);
        }

        [MenuItem(_toolMenu + "/Linux/Spawner")]
        private static void BuildSpawnerForLinux()
        {
            BuildSpawner(_linuxBuildInfo);
        }

        [MenuItem(_toolMenu + "/Linux/Client")]
        private static void BuildClientForLinux()
        {
            BuildClient(_linuxBuildInfo);
        }

        private static void BuildRoom(PlatformBuildInfo platformInfo, bool isHeadless)
        {
            string buildFolder = Path.Combine(_buildFolderName, platformInfo.PlatformName, "Room");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/InternalAssets/Scenes/Room/Room.unity"
                },
                locationPathName = Path.Combine(buildFolder, "Room." + platformInfo.FileExtension),
                target = platformInfo.Target,
#if UNITY_2021_1_OR_NEWER
                options = BuildOptions.ShowBuiltPlayer,// | BuildOptions.Development,
                subtarget = isHeadless ? (int)StandaloneBuildSubtarget.Server : (int)StandaloneBuildSubtarget.Player
#else
                options = isHeadless ? BuildOptions.ShowBuiltPlayer | BuildOptions.EnableHeadlessMode : BuildOptions.ShowBuiltPlayer
#endif
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                string appConfig = Mst.Args.AppConfigFile(buildFolder);

                MstProperties properties = new MstProperties();
                properties.Add(Mst.Args.Names.StartClientConnection, true);
                properties.Add(Mst.Args.Names.MasterIp, Mst.Args.MasterIp);
                properties.Add(Mst.Args.Names.MasterPort, Mst.Args.MasterPort);
                properties.Add(Mst.Args.Names.RoomIp, Mst.Args.RoomIp);
                properties.Add(Mst.Args.Names.RoomPort, Mst.Args.RoomPort);

                File.WriteAllText(appConfig, properties.ToReadableString("\n", "="));

                Debug.Log("Room build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Room build failed");
            }
        }

        private static void BuildMasterAndSpawner(PlatformBuildInfo platformInfo)
        {
            string buildFolder = Path.Combine(_buildFolderName, platformInfo.PlatformName, "MasterAndSpawner");
            string roomExePath = Path.Combine(platformInfo.BuildFolderPath, _buildFolderName, 
                platformInfo.PlatformName, "Room", "Room." + platformInfo.FileExtension);

            if (platformInfo.Target == BuildTarget.StandaloneLinux64)
                roomExePath = roomExePath.Replace('\\', '/');

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/InternalAssets/Scenes/Master/Master.unity" },
                locationPathName = Path.Combine(buildFolder, "MasterAndSpawner." + platformInfo.FileExtension),
                target = platformInfo.Target,
#if UNITY_2021_1_OR_NEWER
                options = BuildOptions.ShowBuiltPlayer, //| BuildOptions.Development,
                subtarget = (int)StandaloneBuildSubtarget.Server
#else
                options = BuildOptions.EnableHeadlessMode | BuildOptions.ShowBuiltPlayer | BuildOptions.Development
#endif
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                MstProperties properties = new MstProperties();
                properties.Add(Mst.Args.Names.StartMaster, true);
                properties.Add(Mst.Args.Names.StartSpawner, true);
                properties.Add(Mst.Args.Names.StartClientConnection, true);
                properties.Add(Mst.Args.Names.MasterIp, Mst.Args.MasterIp);
                properties.Add(Mst.Args.Names.MasterPort, Mst.Args.MasterPort);
                properties.Add(Mst.Args.Names.RoomExecutablePath, roomExePath);
                properties.Add(Mst.Args.Names.RoomIp, Mst.Args.RoomIp);
                properties.Add(Mst.Args.Names.RoomRegion, Mst.Args.RoomRegion);

                File.WriteAllText(Path.Combine(buildFolder, "application.cfg"), properties.ToReadableString("\n", "="));

                Debug.Log("Master Server build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Master Server build failed");
            }
        }

        private static void BuildSpawner(PlatformBuildInfo platformInfo)
        {
            string buildFolder = Path.Combine(_buildFolderName, platformInfo.PlatformName, "Spawner");
            string roomExePath = Path.Combine(platformInfo.BuildFolderPath, _buildFolderName, 
                platformInfo.PlatformName, "Room", "Room." + platformInfo.FileExtension);

            if (platformInfo.Target == BuildTarget.StandaloneLinux64)
                roomExePath = roomExePath.Replace('\\', '/');

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/InternalAssets/Scenes/Spawner/Spawner.unity"
                },
                locationPathName = Path.Combine(buildFolder, "Spawner." + platformInfo.FileExtension),
                target = platformInfo.Target,
#if UNITY_2021_1_OR_NEWER
                options = BuildOptions.ShowBuiltPlayer,// | BuildOptions.Development,
                subtarget = (int)StandaloneBuildSubtarget.Server
#else
                options = BuildOptions.EnableHeadlessMode | BuildOptions.ShowBuiltPlayer | BuildOptions.Development
#endif
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                string appConfig = Mst.Args.AppConfigFile(buildFolder);

                MstProperties properties = new MstProperties();
                properties.Add(Mst.Args.Names.StartSpawner, true);
                properties.Add(Mst.Args.Names.StartClientConnection, true);
                properties.Add(Mst.Args.Names.MasterIp, Mst.Args.MasterIp);
                properties.Add(Mst.Args.Names.MasterPort, Mst.Args.MasterPort);
                properties.Add(Mst.Args.Names.RoomExecutablePath, roomExePath);
                properties.Add(Mst.Args.Names.RoomIp, Mst.Args.RoomIp);
                properties.Add(Mst.Args.Names.RoomRegion, Mst.Args.RoomRegion);

                File.WriteAllText(appConfig, properties.ToReadableString("\n", "="));

                Debug.Log("Spawner build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Spawner build failed");
            }
        }

        private static void BuildClient(PlatformBuildInfo platformInfo)
        {
            string buildFolder = Path.Combine(_buildFolderName, platformInfo.PlatformName, "Client");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/InternalAssets/Scenes/Client/Client.unity",
                    "Assets/InternalAssets/Scenes/Room/Room.unity"
                },
                locationPathName = Path.Combine(buildFolder, "Client." + platformInfo.FileExtension),
                target = platformInfo.Target,
                options = BuildOptions.ShowBuiltPlayer,// | BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                string appConfig = Mst.Args.AppConfigFile(buildFolder);

                MstProperties properties = new MstProperties();
                properties.Add(Mst.Args.Names.StartClientConnection, true);
                properties.Add(Mst.Args.Names.MasterIp, Mst.Args.MasterIp);
                properties.Add(Mst.Args.Names.MasterPort, Mst.Args.MasterPort);

                File.WriteAllText(appConfig, properties.ToReadableString("\n", "="));

                Debug.Log("Client build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Client build failed");
            }
        }
    }
}
#endif