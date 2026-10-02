using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace CoastRun
{
    /// 226차(사용자: 「한번에 AAB 받으면 용량이 크니 나눠서 받게」): 컷씬 그림 대부분을 Play Asset Delivery 「fast-follow」 팩(CutImages)으로 뺐다.
    ///   설치하면 스토어가 본체 뒤에 이 팩을 알아서 이어 받는다. 첫 이야기(오프닝·CS1·EV1·1장 VN)에 쓰는 그림은 본체(Resources)에 남김.
    ///   팩 안 그림 목록: Resources/CoastRun/컷씬이미지/_packindex.txt (컷 id \t 에디터 경로) — 에디터 메뉴 「226 - Build cut image pack」 이 만든다.
    ///   에디터에선 원본(Assets/_CoastRun/CutPackSrc)을 바로 읽고, 폰에선 팩의 cutimages.bundle(없으면 APK 안 StreamingAssets 자리)에서 읽는다.
    public static class CutPack
    {
        public const string PackName = "CutImages";
        public const string BundleFile = "cutimages.bundle";
        static Dictionary<string, string> _ids;   // id → 에디터 경로
        static AssetBundle _bundle; static bool _bundleTried;

        static void EnsureIds()
        {
            if (_ids != null) return;
            _ids = new Dictionary<string, string>(512, System.StringComparer.Ordinal);
            var ta = Resources.Load<TextAsset>(ArtAssets.ResourceRoot + "컷씬이미지/_packindex");
            if (ta == null || string.IsNullOrEmpty(ta.text)) return;
            foreach (var raw in ta.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                int tab = raw.IndexOf('\t'); string id = (tab > 0 ? raw.Substring(0, tab) : raw).Trim().TrimStart('﻿');
                if (id.Length > 0 && !_ids.ContainsKey(id)) _ids[id] = tab > 0 ? raw.Substring(tab + 1).Trim() : "";
            }
            Resources.UnloadAsset(ta);
        }

        public static bool InPack(string id) { if (string.IsNullOrEmpty(id)) return false; EnsureIds(); return _ids.ContainsKey(id); }

        /// 팩 그림을 읽을 수 있는 상태인가(에디터는 늘 true)
        public static bool Ready
        {
            get
            {
#if UNITY_EDITOR
                return true;
#elif UNITY_ANDROID
                return OpenBundle() != null;
#else
                return false;
#endif
            }
        }

        public static Texture2D Load(string id)
        {
            if (!InPack(id)) return null;
#if UNITY_EDITOR
            var p = _ids[id]; if (string.IsNullOrEmpty(p)) return null;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(p);
#else
            var b = OpenBundle(); if (b == null) return null;
            return b.LoadAsset<Texture2D>(id);
#endif
        }

#if !UNITY_EDITOR
        static AssetBundle OpenBundle()
        {
            if (_bundle != null) return _bundle;
            string path = null;
#if UNITY_ANDROID
            try { var pp = AndroidAssetPacks.GetAssetPackPath(PackName); if (!string.IsNullOrEmpty(pp)) { var f = Path.Combine(pp, BundleFile); if (File.Exists(f)) path = f; } } catch { }
#endif
            if (path == null)
            {
                // AAB 가 아닌 APK(테스트용) 빌드면 팩 내용이 APK assets 안에 들어간다
                if (_bundleTried) return null;
                path = Path.Combine(Application.streamingAssetsPath, BundleFile);
            }
            _bundleTried = true;
            try { _bundle = AssetBundle.LoadFromFile(path); } catch { _bundle = null; }
            if (_bundle != null) _bundleTried = false;
            return _bundle;
        }
#endif

        /// 이 장면에 팩 그림이 하나라도 있고 아직 못 받았으면, 받는 동안 「추가 그림 받는 중」 화면을 띄우고 기다린다.
        public static IEnumerator EnsureReady(IEnumerable<string> ids)
        {
            bool need = false;
            if (ids != null) foreach (var id in ids) if (InPack(id)) { need = true; break; }
            if (!need || Ready) yield break;
#if UNITY_ANDROID && !UNITY_EDITOR
            var cv = CoastUiCanvas.Create("CutPackWait", 470);
            var root = CoastUiCanvas.Root(cv);
            CoastHudLayout.MakeImage(root, "Bg", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.03f, 0.03f, 0.08f, 1f)).raycastTarget = true;
            var label = CoastHudLayout.MakeText(root, "T", Loc.T("이야기 그림을 받고 있어요…", "Downloading story pictures…"), 30, TextAnchor.MiddleCenter, new Vector2(0f, 0.45f), new Vector2(1f, 0.6f), new Vector2(40f, 0f), new Vector2(-40f, 0f));
            label.color = new Color(1f, 0.95f, 0.85f);
            bool askedData = false; AndroidAssetPackInfo last = null; float retryAt = 0f;
            AndroidAssetPacks.DownloadAssetPackAsync(new[] { PackName }, i => last = i);
            while (!Ready)
            {
                var info = last;
                if (info != null)
                {
                    float pr = info.size > 0 ? (float)info.bytesDownloaded / info.size : 0f;
                    var st = info.status;
                    if (st == AndroidAssetPackStatus.WaitingForWifi && !askedData) { askedData = true; AndroidAssetPacks.RequestToUseMobileDataAsync(r => { }); }
                    if (st == AndroidAssetPackStatus.Failed || st == AndroidAssetPackStatus.Canceled)
                    {
                        label.text = Loc.T("받기가 멈췄어요. 인터넷 연결을 확인해 주세요 — 다시 시도하는 중…", "Download stopped. Check your connection — retrying…");
                        if (Time.unscaledTime > retryAt) { retryAt = Time.unscaledTime + 4f; last = null; AndroidAssetPacks.DownloadAssetPackAsync(new[] { PackName }, i => last = i); }
                    }
                    else
                        label.text = Loc.T($"이야기 그림을 받고 있어요… {Mathf.RoundToInt(pr * 100f)}%", $"Downloading story pictures… {Mathf.RoundToInt(pr * 100f)}%")
                                   + (st == AndroidAssetPackStatus.WaitingForWifi ? Loc.T("\n(와이파이를 기다리는 중)", "\n(Waiting for Wi-Fi)") : "");
                }
                yield return null;
            }
            if (cv != null) Object.Destroy(cv.gameObject);
#else
            yield break;
#endif
        }

        /// 앱을 켤 때 한 번 — 아직 안 받았으면 미리 받기 시작(fast-follow 는 스토어가 알아서 받지만, 중간에 끊겼을 때 다시 잇기)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Prefetch()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { if (string.IsNullOrEmpty(AndroidAssetPacks.GetAssetPackPath(PackName))) AndroidAssetPacks.DownloadAssetPackAsync(new[] { PackName }); } catch { }
#endif
        }

        /// 컷씬(CinematicTable)·VN(ChapterScript) 이 쓰는 그림 id
        public static IEnumerable<string> IdsOf(CinematicTable.Def def)
        {
            if (def == null || def.cuts == null) yield break;
            foreach (var c in def.cuts) { if (c == null) continue; if (!string.IsNullOrEmpty(c.still)) yield return c.still; if (!string.IsNullOrEmpty(c.fallback)) yield return c.fallback; }
        }
        public static IEnumerable<string> IdsOf(VnLine[] lines, string sceneId = null)
        {
            if (!string.IsNullOrEmpty(sceneId)) yield return "Cut_" + sceneId;   // 그림 없는 BG(Blank) 자리의 씬 컷
            if (lines == null) yield break;
            foreach (var l in lines) if (l.Kind == "CG" && !string.IsNullOrEmpty(l.A)) yield return "Cut_" + l.A;
        }
    }
}
