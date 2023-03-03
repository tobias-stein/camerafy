using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;

namespace Camerafy.Editor.Build
{
    public class Builder
    {
        public static readonly string DefaultApplicationName = "Camerafy";
        public static readonly BuildOptions DefaultBuildOptions = BuildOptions.None;


        [MenuItem("Camerafy/Build Camerafy")]
        public static void BuildCamerafy()
        {
            // get final build location
            DirectoryInfo BuildDir = new DirectoryInfo(Path.Combine(UnityEngine.Application.dataPath, "..", $"Camerafy-{UnityEngine.Application.version}"));


            // Clear build directory
            {
                if (BuildDir.Exists)
                    BuildDir.Delete(true);
                BuildDir.Create();
            }

            // setup players build options
            BuildPlayerOptions BPO = new BuildPlayerOptions
            {
                locationPathName = Path.Combine(BuildDir.FullName, DefaultApplicationName + ".exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = DefaultBuildOptions,
                scenes = new string[]
                {
                    UnityEngine.Application.dataPath + "/Scenes/StartUp.unity"
                },
            };

            // Build the player.
            BuildPipeline.BuildPlayer(BPO);
        }

        #region POST BUILD STEPS

        [PostProcessBuild(1)]
        public static void CopyAdditialProgramData(BuildTarget target, string pathToBuiltProject)
        {
            string SrcRootPath = new DirectoryInfo(Path.Combine(UnityEngine.Application.dataPath, "..")).FullName;
            string DstRootPath = Path.GetDirectoryName(pathToBuiltProject);

            // Copy Configs
            CopyTarget("Config", SrcRootPath, DstRootPath);

            // Copy Prerequisites
            CopyTarget("Prerequisites", SrcRootPath, DstRootPath);

            // Copy Web application
            CopyTarget("WebApplication", SrcRootPath, DstRootPath,
                new[] {
                ".gitignore",
                ".vscode",
                //"db.sqlite3",
                "__pycache__",
                "node_modules"
                });

            // Copy Scripts
            {
                //CopyTarget(Path.Combine("Scripts", "Create_SSL_Certificate.bat"), SrcRootPath, DstRootPath, true);
            }

            // Copy Programs
            {
                //CopyTarget(Path.Combine("Programs", "makecert.exe"), SrcRootPath, DstRootPath, true);
            }

            // Copy EULA and README
            {
                CopyTarget("EULA.txt", SrcRootPath, DstRootPath, true);
                CopyTarget("README.txt", SrcRootPath, DstRootPath, true);
            }
        }

        [PostProcessBuild(9999)]
        public static void OpenBuildDir(BuildTarget target, string pathToBuiltProject)
        {
            Process.Start(Path.GetDirectoryName(pathToBuiltProject));
        }

        #endregion // POST BUILD STEPS

        #region HELPER METHODS

        private static void CopyTarget(string Target, string From, string To, bool IsFile = false)
        {
            string CopyFrom = Path.Combine(From, Target);
            string CopyTo = Path.Combine(To, Target);

            if (IsFile)
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(CopyTo));
                if (!di.Exists)
                    di.Create();
            }

            FileUtil.CopyFileOrDirectory(CopyFrom, CopyTo);
        }

        private static void CopyTarget(string Target, string From, string To, string[] exclude)
        {
            string CopyFrom = Path.Combine(From, Target);
            string pattern = string.Format("({0})", String.Join("|", exclude));

            foreach (var filepath in Directory.EnumerateFiles(CopyFrom, "*.*", SearchOption.AllDirectories))
            {
                if (Regex.IsMatch(filepath, pattern, RegexOptions.Singleline))
                    continue;

                string CopyTo = Path.Combine(To, Target, filepath.Substring(CopyFrom.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(CopyTo));
                new FileInfo(filepath).CopyTo(CopyTo);
            }
        }

        #endregion // HELPER METHODS
    }
}