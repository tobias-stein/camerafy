using UnityEditor;

namespace Camerafy.Editor.Build
{
    using Application.Config;

    public class DefaultConfigGenerator
    {

        [MenuItem("Camerafy/Util/Generate default configuration")]
        public static void GenerateDefaultConfig()
        {
            Configuration Config = new Configuration();

            Serialization.EntityManager.SaveEntity(Config, Config.ConfigurationFilepath);

            UnityEngine.Debug.Log($"Generate a new default configuration file in '{Config.ConfigurationFilepath}'.");
        }
    }
}