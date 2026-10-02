using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Loads MCP-exported art from Resources/CoastRun.
    public static class ArtAssets
    {
        public const string ResourceRoot = "CoastRun/";
        const string CutsceneImageRoot = ResourceRoot + "컷씬이미지";

        /// Legacy id (e.g. Cut_T_OP_01) → texture after files were renamed under 컷씬이미지/.
        /// 220차: 불러온 컷 그림만 담는 캐시(예전엔 494장 전부를 한 번에 올려 첫 컷씬이 몇 초 멈췄다).
        static Dictionary<string, Texture2D> _cutByLegacyId;
        /// 220차: 컷 id → Resources 경로(에디터가 만든 컷씬이미지/_index.txt). 없으면 옛 방식(전부 읽기)으로.
        static Dictionary<string, string> _cutPath;

        public static Texture2D LoadTexture(string fileNameWithoutExt)
        {
            var tex = Resources.Load<Texture2D>(ResourceRoot + fileNameWithoutExt);
            if (tex != null)
                return tex;

            if (!string.IsNullOrEmpty(fileNameWithoutExt) &&
                fileNameWithoutExt.StartsWith("Cut_", StringComparison.Ordinal))
            {
                EnsureCutsceneIndex();
                if (_cutByLegacyId != null && _cutByLegacyId.TryGetValue(fileNameWithoutExt, out tex) && tex != null)
                    return tex;
                if (_cutPath != null && _cutPath.TryGetValue(fileNameWithoutExt, out var path))
                {
                    tex = Resources.Load<Texture2D>(path);
                    if (tex != null) { _cutByLegacyId[fileNameWithoutExt] = tex; return tex; }
                }
                // 226차: 본체에 없으면 fast-follow 팩(CutImages)에서
                if (CutPack.InPack(fileNameWithoutExt))
                {
                    tex = CutPack.Load(fileNameWithoutExt);
                    if (tex != null) { _cutByLegacyId[fileNameWithoutExt] = tex; return tex; }
                }
            }

            tex = CoastSceneArt.Load(fileNameWithoutExt);
            if (tex != null)
                return tex;

            switch (fileNameWithoutExt)
            {
                case "UI_TitleBackground": return CoastUiArt.TitleBackground;
                case "UI_CharacterHero":
                    return CoastUiArt.LoadOrFallback("UI_TitleBackground", () => CoastUiArt.TitleBackground);
                case "Icon_Coin": return CoastUiArt.CoinIcon;
                case "Icon_Speed": return CoastUiArt.SpeedIcon;
                case "Icon_Magnet": return CoastUiArt.MagnetIcon;
                case "Icon_Tower": return CoastUiArt.TowerIcon;
                case "Icon_Him": return CoastUiArt.HimIcon;
                case "UI_Panel_Memory": return CoastUiArt.MemoryPanel;
                case "Watch_Frame": return CoastUiArt.WatchFrame;
                default: return null;
            }
        }

        static void EnsureCutsceneIndex()
        {
            if (_cutByLegacyId != null)
                return;

            _cutByLegacyId = new Dictionary<string, Texture2D>(256, StringComparer.Ordinal);

            // 220차: 목록(_index)이 있으면 경로만 읽어 두고, 그림은 쓸 때 한 장씩.
            var index = Resources.Load<TextAsset>(CutsceneImageRoot + "/_index");
            if (index != null && !string.IsNullOrEmpty(index.text))
            {
                _cutPath = new Dictionary<string, string>(512, StringComparer.Ordinal);
                foreach (var raw in index.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int tab = raw.IndexOf('\t'); if (tab <= 0) continue;
                    var id = raw.Substring(0, tab).Trim().TrimStart('\uFEFF');
                    if (!_cutPath.ContainsKey(id)) _cutPath[id] = raw.Substring(tab + 1).Trim();
                }
                if (_cutPath.Count > 0) { Resources.UnloadAsset(index); return; }
                _cutPath = null;
            }

            // Resources.LoadAll is not recursive — load each folder listed in _paths.txt.
            var pathList = Resources.Load<TextAsset>(CutsceneImageRoot + "/_paths");
            if (pathList != null && !string.IsNullOrEmpty(pathList.text))
            {
                var lines = pathList.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (var p = 0; p < lines.Length; p++)
                {
                    var folder = lines[p].Trim();
                    if (folder.Length == 0)
                        continue;
                    IndexCutTextures(Resources.LoadAll<Texture2D>(folder));
                }
            }
            else
            {
                IndexCutTextures(Resources.LoadAll<Texture2D>(CutsceneImageRoot));
            }
        }

        static void IndexCutTextures(Texture2D[] all)
        {
            if (all == null || all.Length == 0)
                return;

            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || string.IsNullOrEmpty(t.name))
                    continue;

                var idx = t.name.LastIndexOf("Cut_", StringComparison.Ordinal);
                if (idx < 0)
                    continue;

                var legacyId = t.name.Substring(idx);
                if (!_cutByLegacyId.ContainsKey(legacyId))
                    _cutByLegacyId[legacyId] = t;
            }
        }

        public static Material CreateTexturedUnlit(Texture2D tex, Color tint)
        {
            var mat = CoastMaterials.CreateUnlit(tint);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            return mat;
        }

        public static Material CreateTexturedLit(Texture2D tex, Color tint, float smoothness = 0.1f)
        {
            var mat = CoastMaterials.CreateLit(tint, smoothness);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            return mat;
        }

        public static GameObject LoadPrefabOrNull(string resourceName)
        {
            return Resources.Load<GameObject>(ResourceRoot + resourceName);
        }
    }
}
