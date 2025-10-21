using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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
                    StreamingAssetsToResourceBuilderSettingsWindow.Embed();
            }
        }
    }
}