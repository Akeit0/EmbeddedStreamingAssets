using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using File = System.IO.File;

namespace EmbeddedStreamingAssets
{
    [PreferBinarySerialization]
    public class EmbeddedAssets : ScriptableObject
    {
        public static EmbeddedAssets Instance { get; private set; }

        [Serializable]
        struct AssetEntry
        {
            public string key;
            public int offset;
            public int length;
        }

        [SerializeField] AssetEntry[] entries;


        [SerializeField] private TextAsset textAsset;

        (int Hash, int Index)[] entriesMap;


#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/EmbeddedAssets")]
        public static void CreateAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save EmbeddedAssets",
                "EmbeddedAssets",
                "asset",
                string.Empty);

            if (string.IsNullOrEmpty(path))
                return;

            var newSettings = CreateInstance<EmbeddedAssets>();
            AssetDatabase.CreateAsset(newSettings, path);
            var bytesPath = Path.Combine(Path.GetDirectoryName(path)!, "EmbeddedAssets.bytes");
            File.WriteAllBytes(bytesPath, Array.Empty<byte>());
            AssetDatabase.ImportAsset(bytesPath);

            newSettings.textAsset = (TextAsset)AssetDatabase.LoadAssetAtPath(bytesPath, typeof(TextAsset));

            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().AsEnumerable();
            preloadedAssets = preloadedAssets.Where(x => x is { } and not EmbeddedAssets);
            preloadedAssets = preloadedAssets.Append(newSettings);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }

        public static void LoadInstanceFromPreloadAssets()
        {
            if (Instance != null)
                return;
            var preloadAsset = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is EmbeddedAssets);
            if (preloadAsset is EmbeddedAssets instance)
            {
                instance.OnDisable();
                instance.OnEnable();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitialize()
        {
            // For editor, we need to load the Preload asset manually.
            LoadInstanceFromPreloadAssets();
        }

        [InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            LoadInstanceFromPreloadAssets();
        }

        void ClearAssets()
        {
            entries = null;
        }

        public static void RegisterAssets(IEnumerable<(string path, string entry)> assets) => Instance?.RegisterAssetsImpl(assets);

        void RegisterAssetsImpl(IEnumerable<(string path, string entry)> assets)
        {
            ClearAssets();
            var tempEntries = ArrayPool<AssetEntry>.Shared.Rent(16);
            int count = 0;
            var tempData = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            int totalDataLength = 0;
            try
            {
                foreach (var asset in assets)
                {
                    var path = asset.path;
                    var entry = asset.entry;
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    if (count >= tempEntries.Length)
                    {
                        var newSize = Mathf.NextPowerOfTwo(count + 1);
                        var newEntries = ArrayPool<AssetEntry>.Shared.Rent(newSize);
                        Array.Copy(tempEntries, 0, newEntries, 0, count);
                        ArrayPool<AssetEntry>.Shared.Return(tempEntries);
                        tempEntries = newEntries;
                    }

                    var dataLength = (int)fs.Length;

                    if (totalDataLength + dataLength > tempData.Length)
                    {
                        var newSize = Mathf.NextPowerOfTwo(totalDataLength + dataLength);
                        var newData = ArrayPool<byte>.Shared.Rent(newSize);
                        Array.Copy(tempData, 0, newData, 0, totalDataLength);
                        ArrayPool<byte>.Shared.Return(tempData);
                        tempData = newData;
                    }

                    var actual = fs.Read(tempData.AsSpan(totalDataLength, dataLength));
                    tempEntries[count++] = new AssetEntry
                    {
                        key = entry,
                        offset = totalDataLength,
                        length = actual
                    };

                    totalDataLength += actual;
                }

                entries = new AssetEntry[count];
                Array.Copy(tempEntries, 0, entries, 0, count);

                using (var fileStream = new FileStream(AssetDatabase.GetAssetPath(textAsset), FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(tempData, 0, totalDataLength);
                }

                Debug.Log($"InMemoryAssets: Registered {count} assets, total size: {totalDataLength} bytes");
            }
            finally
            {
                ArrayPool<AssetEntry>.Shared.Return(tempEntries);
                ArrayPool<byte>.Shared.Return(tempData);
            }
        }
#endif
        void OnEnable()
        {
            Instance = this;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        void InitSearchEntries()
        {
            if (entriesMap != null)
                return;
            entriesMap = new (int, int)[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entriesMap[i] = (entries[i].key.GetHashCode(), i);
            }

            Array.Sort(entriesMap, (a, b) => a.Hash.CompareTo(b.Hash));
        }

        public static bool TryGetAssetData(string key, out NativeArray<byte> result) => Instance.TryGetAssetDataImpl(key, out result);
        bool TryGetAssetDataImpl(string key, out NativeArray<byte> result)
        {
            result = default;
            if (entries == null || textAsset == null)
                return false;
            InitSearchEntries();

            var keyHash = key.GetHashCode();
            int left = 0;
            int right = entriesMap.Length - 1;
            AssetEntry entry = default;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var (hash, index) = entriesMap[mid];
                if (hash != keyHash)
                {
                    if (hash < keyHash)
                    {
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }
                else
                {
                    // Handle hash collision by searching adjacent entries
                    for (int delta = -1; delta <= 1; delta += 2)
                    {
                        int scan = mid;
                        while ((scan >= left && scan <= right))
                        {
                            (hash, index) = entriesMap[scan];
                            if (hash != keyHash)
                                break;
                            var entryToCheck = entries[index];
                            if (entryToCheck.key == key)
                            {
                                entry = entryToCheck;
                                goto End;
                            }

                            scan += delta;
                        }
                    }

                    break;
                }
            }

        End:

            if (entry.key == null)
                return false;
            var bytes = textAsset.GetData<byte>();

            result = bytes.GetSubArray(entry.offset, entry.length);
            return true;
        }

#endif
        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}