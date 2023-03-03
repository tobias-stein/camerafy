using UnityEngine;
using UnityEditor;
using System.IO;

namespace Camerafy.Editor.Util
{
    [InitializeOnLoad]
    public static class FrontendConfigUpdater
    {
        static FrontendConfigUpdater()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Service.JSCompiler.JSCompiler.Compile();

                string AppConfigName = "AppConfig.json";
                string dest = Path.Combine(UnityEngine.Application.dataPath, "..", "WebApplication", "frontend", AppConfigName);
                if (File.Exists(dest))
                    File.Delete(dest);

                FileUtil.CopyFileOrDirectory(
                    Path.Combine(UnityEngine.Application.dataPath, "..", "Config", AppConfigName),
                    dest);
            }
        }
    }
}