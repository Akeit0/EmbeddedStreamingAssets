using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmbeddedStreamingAssets;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace EmbeddedStreamingAssets.Editor
{
    internal class BuildPreProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                if (!StreamingAssetsToResourceBuilderSettingsWindow.SkipEmbeddingOnBuild)
                    Embed();
            }
        }

        public static void Embed()
        {
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            IEnumerable<string> files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
            var addressablesPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, (StreamingAssetsToResourceBuilderSettingsWindow.AddressableBuildPath));
            if (Directory.Exists(addressablesPath))
            {
                Debug.Log("Found Addressables Build at: " + addressablesPath);
                var addressableFiles = Directory.GetFiles(addressablesPath, "*", SearchOption.AllDirectories);
                files = files.Concat(addressableFiles);
            }
            files =files.Where( f => !f.EndsWith(".meta"));
            
            EmbeddedAssets embeddedAssets = EmbeddedAssets.Instance;
            if(embeddedAssets != null)
            {
                embeddedAssets.RegisterAssets(files.Select( file =>
                {
                    var rel = file.StartsWith(addressablesPath)
                        ? "aa/" + file[(addressablesPath.Length + 1)..].Replace("\\", "/")
                        : file[(streamingAssetsPath.Length + 1)..].Replace("\\", "/"); // e.g. "a/bundle.data"
                    return (file, rel);
                }));
            }
            AssetDatabase.Refresh();
        }
    }
}