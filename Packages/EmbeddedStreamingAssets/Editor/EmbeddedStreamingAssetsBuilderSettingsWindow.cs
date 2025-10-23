using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EmbeddedStreamingAssets.Editor
{
    internal class EmbeddedStreamingAssetsBuilderSettingsWindow : EditorWindow
    {
        public static string AddressableBuildPath
        {
            get => data.AddressableBuildPath;
            private set => data.AddressableBuildPath = value;
        }

        public static bool SkipEmbeddingOnBuild
        {
            get => data.SkipEmbeddingOnBuild;
            private set => data.SkipEmbeddingOnBuild = value;
        }

        const string JsonFilePath = "ProjectSettings/EmbeddedStreamingAssetsBuilderSettings.json";

        class Data
        {
            // ReSharper disable MemberHidesStaticFromOuterClass
            public string AddressableBuildPath = "Library/com.unity.addressables/aa/WebGL";
            public bool SkipEmbeddingOnBuild;
        }

        static Data data = new();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (File.Exists(JsonFilePath))
            {
                var json = File.ReadAllText(JsonFilePath);
                data = JsonUtility.FromJson<Data>(json);
            }
        }

        [MenuItem("Window/EmbeddedStreamingAssets/Build Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<EmbeddedStreamingAssetsBuilderSettingsWindow>();
            window.titleContent = new GUIContent("EmbeddedStreamingAssets Build Settings");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        void OnGUI()
        {
            AddressableBuildPath = EditorGUILayout.TextField("AddressableBuildPath", AddressableBuildPath);
            
            if (GUILayout.Button("Embed Assets"))
            {
                Embed();
            }

            SkipEmbeddingOnBuild = EditorGUILayout.Toggle("Skip Auto Embedding On Build", SkipEmbeddingOnBuild);
        }

        public static void Embed()
        {
            AssetDatabase.SaveAssets();
            var dstRoot = Path.Combine(Application.dataPath, "Resources/EmbeddedSA");
            if (Directory.Exists(dstRoot)) Directory.Delete(dstRoot, true);
            Directory.CreateDirectory(dstRoot);
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            IEnumerable<string> files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
            var addressablesPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, (EmbeddedStreamingAssetsBuilderSettingsWindow.AddressableBuildPath));
            if (Directory.Exists(addressablesPath))
            {
                Debug.Log("Found Addressables Build at: " + addressablesPath);
                var addressableFiles = Directory.GetFiles(addressablesPath, "*", SearchOption.AllDirectories);
                files = files.Concat(addressableFiles);
            }

            files = files.Where(f => !f.EndsWith(".meta"));


            foreach (var file in files)
            {
                var relativePath = file.StartsWith(addressablesPath)
                    ? "aa/" + file[(addressablesPath.Length + 1)..]
                    : file[(streamingAssetsPath.Length + 1)..];
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

        void OnDisable()
        {
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(JsonFilePath, json);
        }
    }
}