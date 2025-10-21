using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EmbeddedStreamingAssets.Editor
{
    internal class StreamingAssetsToResourceBuilderSettingsWindow : EditorWindow
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

        const string JsonFilePath = "ProjectSettings/StreamingAssetsToResourceBuilderSetting.json";

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
            var window = GetWindow<StreamingAssetsToResourceBuilderSettingsWindow>();
            window.titleContent = new GUIContent("EmbeddedStreamingAssets Build Settings");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        void OnGUI()
        {
            AddressableBuildPath = EditorGUILayout.TextField("AddressableBuildPath", AddressableBuildPath);

            if (EmbeddedAssets.Instance == null)
            {
                if (GUILayout.Button("Create Embedded Assets"))
                {
                    EmbeddedAssets.CreateAsset();
                }
            }
            else
            {
                if (GUILayout.Button("Embed Assets"))
                {
                    Embed();
                }

                SkipEmbeddingOnBuild = EditorGUILayout.Toggle("Skip Auto Embedding On Build", SkipEmbeddingOnBuild);
            }
        }
        public static void Embed()
        {
            AssetDatabase.SaveAssets();
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            IEnumerable<string> files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
            var addressablesPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, (AddressableBuildPath));
            if (Directory.Exists(addressablesPath))
            {
                Debug.Log("Found Addressables Build at: " + addressablesPath);
                var addressableFiles = Directory.GetFiles(addressablesPath, "*", SearchOption.AllDirectories);
                files = files.Concat(addressableFiles);
            }

            files = files.Where(f => !f.EndsWith(".meta"));

            EmbeddedAssets.RegisterAssets(files.Select(file =>
            {
                var rel = file.StartsWith(addressablesPath)
                    ? "aa/" + file[(addressablesPath.Length + 1)..].Replace("\\", "/")
                    : file[(streamingAssetsPath.Length + 1)..].Replace("\\", "/"); // e.g. "a/bundle.data"
                return (file, rel);
            }));
            AssetDatabase.Refresh();
        }
        void OnDisable()
        {
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(JsonFilePath, json);
        }
    }
}