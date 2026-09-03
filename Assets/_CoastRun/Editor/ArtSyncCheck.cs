#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Assets/_CoastRun/Art holds the authoring originals; Assets/Resources/CoastRun
    /// holds the runtime-loadable copies (see Art/README.md). Only the Resources copy
    /// ships, so an edited original that was never re-copied silently ships stale art.
    /// This check reports pairs whose bytes have drifted apart.
    public static class ArtSyncCheck
    {
        private const string ArtRoot = "Assets/_CoastRun/Art";
        private const string ResRoot = "Assets/Resources/CoastRun";

        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".wav", ".ogg" };

        [MenuItem("Coast Run/Check Art ↔ Resources Sync")]
        public static void Check()
        {
            var drifted = new List<string>();
            var matched = 0;

            foreach (string original in EnumerateArtFiles())
            {
                string copy = Path.Combine(ResRoot, Path.GetFileName(original)).Replace('\\', '/');
                if (!File.Exists(copy))
                    continue;

                if (Hash(original) == Hash(copy))
                    matched++;
                else
                    drifted.Add(Path.GetFileName(original));
            }

            if (drifted.Count == 0)
            {
                Debug.Log($"Art sync OK — {matched} pair(s) identical.");
                return;
            }

            Debug.LogWarning(
                $"Art sync DRIFT — {drifted.Count} of {matched + drifted.Count} pair(s) differ. " +
                "The Resources copy is what ships; re-copy from Art or the build uses stale art.\n  " +
                string.Join("\n  ", drifted));
        }

        [MenuItem("Coast Run/Sync Art → Resources (overwrite copies)")]
        public static void Sync()
        {
            var updated = new List<string>();

            foreach (string original in EnumerateArtFiles())
            {
                string copy = Path.Combine(ResRoot, Path.GetFileName(original)).Replace('\\', '/');
                if (!File.Exists(copy) || Hash(original) == Hash(copy))
                    continue;

                // Overwrite content only. The .meta stays put so the Resources GUID —
                // and every prefab/material pointing at it — survives.
                File.Copy(original, copy, true);
                updated.Add(Path.GetFileName(original));
            }

            if (updated.Count == 0)
            {
                Debug.Log("Art sync: nothing to update.");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Art sync: overwrote {updated.Count} Resources copy(ies).\n  " +
                      string.Join("\n  ", updated));
        }

        private static IEnumerable<string> EnumerateArtFiles()
        {
            if (!Directory.Exists(ArtRoot))
                yield break;

            foreach (string path in Directory.GetFiles(ArtRoot, "*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (Array.IndexOf(Extensions, ext) < 0)
                    continue;
                yield return path.Replace('\\', '/');
            }
        }

        private static string Hash(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(md5.ComputeHash(stream));
        }
    }
}
#endif
