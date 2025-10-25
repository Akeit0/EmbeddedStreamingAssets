using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if ADDRESSABLES_SUPPORT
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
#endif

namespace EmbeddedStreamingAssets.Editor
{
    public class EmbeddedStreamingAssetsBuildWindow : EditorWindow
    {
        [MenuItem("Window/EmbeddedStreamingAssets/Build Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<EmbeddedStreamingAssetsBuildWindow>();
            window.titleContent = new GUIContent("EmbeddedStreamingAssets Build");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        void OnGUI()
        {
#if ADDRESSABLES_SUPPORT
            if (GUILayout.Button("Embed Assets"))
            {
                Embed();
            }

            if (GUILayout.Button("Build Addressables with Embedding"))
            {
                if (BuildAddressables())
                {
                    Embed();
                }
            }

            if (GUILayout.Button("Build Addressables and Player with Embedding"))
            {
                BuildAddressablesAndPlayer();
            }
#else
            if (GUILayout.Button("Embed Assets"))
            {
                    Embed();
            }
#endif
        }

        [MenuItem("Tools/EmbeddedStreamingAssets/Embed Assets")]
        public static void Embed()
        {
            AssetDatabase.SaveAssets();
            var dstRoot = Path.Combine(Application.dataPath, "Resources/EmbeddedSA");
            if (Directory.Exists(dstRoot)) Directory.Delete(dstRoot, true);
            Directory.CreateDirectory(dstRoot);
            var streamingAssetsPath = Application.streamingAssetsPath;
            var addressablesPath = Addressables.BuildPath;

            IEnumerable<string> files;
            {
                files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                if (Directory.Exists(addressablesPath))
                {
                    Debug.Log("Found Addressables Build at: " + addressablesPath);
                    var addressableFiles = Directory.GetFiles(addressablesPath, "*", SearchOption.AllDirectories);
                    files = files.Concat(addressableFiles);
                }

                files = files.Where(f => !f.EndsWith(".meta"));
            }

            foreach (var file in files)
            {
                var relativePath = file switch
                {
                    _ when file.StartsWith(addressablesPath) => "aa/" + file[(addressablesPath.Length + 1)..],
                    _ when file.StartsWith(streamingAssetsPath) => file[(streamingAssetsPath.Length + 1)..],
                    _ => throw new System.Exception("File is not in Addressables or StreamingAssets: " + file)
                };
                var dstPath = Path.Combine(dstRoot, relativePath) + ".bytes";
                var dstDir = Path.GetDirectoryName(dstPath);
                if (!Directory.Exists(dstDir))
                {
                    Directory.CreateDirectory(dstDir!);
                }

                File.Copy(file, dstPath, true);
            }

            AssetDatabase.Refresh();
        }
#if ADDRESSABLES_SUPPORT
        [MenuItem("Tools/EmbeddedStreamingAssets/Build Addressables with Embedding")]
        public static void BuildAddressablesAndEmbed()
        {
            if (BuildAddressables())
            {
                Embed();
            }
        }

        static bool BuildAddressables()
        {
            AssetDatabase.Refresh();

            AddressableAssetSettings
                .BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
            }

            return success;
        }

        [MenuItem("Tools/EmbeddedStreamingAssets/Build Addressables and Player with Embedding")]
        public static void BuildAddressablesAndPlayer()
        {
            var options = new BuildPlayerOptions();
            BuildPlayerOptions playerSettings
                = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
            if (playerSettings.target != BuildTarget.WebGL)
            {
                Debug.LogError("This build process only supports Web target.");
                return;
            }

            bool contentBuildSucceeded = BuildAddressables();

            if (contentBuildSucceeded)
            {
                Embed();

                BuildPipeline.BuildPlayer(playerSettings);
            }
        }
#endif
    }
}