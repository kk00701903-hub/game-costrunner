using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 224차(222차 점검: 일본어 말풍선 글씨가 늘 최소 크기): Jua 폰트에 일본어 글자가 없어 OS 대체 글꼴로 그려지는데,
    /// 이때 Unity 「Best Fit」 계산이 실패해 일본어 글자는 언제나 최소 크기(13)로 떨어진다(같은 칸 ko·en 은 22).
    /// 일본어일 때만 Best Fit 을 끄고, 같은 범위(최소~최대) 안에서 칸에 들어가는 가장 큰 크기를 직접 골라 준다.
    /// 다른 언어로 바뀌면 원래 설정(Best Fit 켜짐·원래 글자 크기)으로 되돌린다. 폰트·용량은 그대로.
    public static class JaBestFit
    {
        sealed class Entry { public int min, max, origSize; public string lastText; public Vector2 lastRect; }
        static readonly Dictionary<Text, Entry> _map = new Dictionary<Text, Entry>();
        static float _scanT; static bool _hooked, _wasJa;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (_hooked) return; _hooked = true;
            Canvas.willRenderCanvases += Tick;
        }

        static void Tick()
        {
            bool ja = Loc.Lang == "ja";
            if (!ja)
            {
                if (_wasJa) Restore();
                _wasJa = false; return;
            }
            _wasJa = true;
            float now = Time.unscaledTime;
            if (now >= _scanT)
            {
                _scanT = now + 0.25f;   // 새로 생긴 Best Fit 글자 찾기(가벼움)
                foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                {
                    if (t == null || !t.resizeTextForBestFit || _map.ContainsKey(t)) continue;
                    _map[t] = new Entry { min = t.resizeTextMinSize, max = t.resizeTextMaxSize, origSize = t.fontSize };
                    t.resizeTextForBestFit = false;
                }
            }
            List<Text> dead = null;
            foreach (var kv in _map)
            {
                var t = kv.Key; var e = kv.Value;
                if (t == null) { (dead ??= new List<Text>()).Add(t); continue; }
                if (!t.isActiveAndEnabled) continue;
                var r = t.rectTransform.rect.size;
                if (e.lastText == t.text && e.lastRect == r) continue;
                e.lastText = t.text; e.lastRect = r;
                Fit(t, e, r);
            }
            if (dead != null) foreach (var d in dead) _map.Remove(d);
        }

        static void Fit(Text t, Entry e, Vector2 box)
        {
            if (box.x < 4f || box.y < 4f || string.IsNullOrEmpty(t.text)) { t.fontSize = e.max; return; }
            var gen = t.cachedTextGeneratorForLayout; float ppu = t.pixelsPerUnit; if (ppu <= 0f) ppu = 1f;
            bool wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap;
            int lo = e.min, hi = e.max, best = e.min;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var s = t.GetGenerationSettings(box); s.resizeTextForBestFit = false; s.fontSize = mid;
                float h = gen.GetPreferredHeight(t.text, s) / ppu;
                bool ok = h <= box.y + 0.5f;
                if (ok && !wrap) ok = gen.GetPreferredWidth(t.text, s) / ppu <= box.x + 0.5f;
                if (ok) { best = mid; lo = mid + 1; } else hi = mid - 1;
            }
            t.fontSize = best;
        }

        static void Restore()
        {
            foreach (var kv in _map) { var t = kv.Key; if (t == null) continue; t.resizeTextForBestFit = true; t.resizeTextMinSize = kv.Value.min; t.resizeTextMaxSize = kv.Value.max; t.fontSize = kv.Value.origSize; }
            _map.Clear();
        }
    }
}
