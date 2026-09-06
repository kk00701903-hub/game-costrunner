#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Every clip under Resources/CoastRun/BGM is a long music track: stream it, Vorbis
    /// it, and don't preload. 425 MB of generated WAV becomes ~40 MB in the build and
    /// stops eating RAM at scene load. Runs automatically on import.
    public class BgmImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/CoastRun/BGM/";

        private const string SfxFolder = "Assets/Resources/CoastRun/SFX/";

        private void OnPreprocessAudio()
        {
            string path = assetPath.Replace('\\', '/');
            if (path.StartsWith(SfxFolder))
            {
                // 짧은 스팅어: 메모리에 압축 해제, 지연 없이 재생.
                var si = (AudioImporter)assetImporter;
                var ss = si.defaultSampleSettings;
                ss.loadType = AudioClipLoadType.DecompressOnLoad;
                ss.compressionFormat = AudioCompressionFormat.Vorbis;
                ss.quality = 0.6f;
                ss.preloadAudioData = true;
                si.defaultSampleSettings = ss;
                si.forceToMono = true;
                si.loadInBackground = false;
                return;
            }
            if (!path.StartsWith(Folder))
                return;

            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            s.preloadAudioData = false;
            importer.defaultSampleSettings = s;
            importer.forceToMono = false;
            importer.loadInBackground = true;
        }

        [MenuItem("Coast Run/BGM/Reimport BGM clips (apply streaming settings)")]
        public static void ReimportAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder.TrimEnd('/') }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log("BGM clips reimported with streaming/Vorbis settings.");
        }
    }
}
#endif
