using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 11차: 실기기 진단 — 빌드에서 에러/예외가 나면 화면 아래에 빨간 띠로 마지막 4줄을 20초 보여준다.
    /// (에디터는 콘솔이 있으니 끔.) 스토어 출시 전엔 SHOW 를 false 로.
    public class CoastErrorOverlay : MonoBehaviour
    {
        public static bool SHOW = true;
        static CoastErrorOverlay _i;
        readonly List<string> _lines = new List<string>();
        Text _text; CanvasGroup _cg; float _hideAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if !UNITY_EDITOR
            if (!SHOW || _i != null) return;
            var go = new GameObject("CoastErrorOverlay");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<CoastErrorOverlay>();
#endif
        }

        void Awake()
        {
            var canvas = CoastUiCanvas.Create("ErrorOverlayCanvas", 900, transform);
            var root = CoastUiCanvas.Root(canvas);
            var bg = CoastHudLayout.MakeImage(root, "Bg", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 150f), new Color(0.6f, 0.05f, 0.05f, 0.85f));
            bg.raycastTarget = false;
            _cg = bg.gameObject.AddComponent<CanvasGroup>(); _cg.alpha = 0f;
            _text = CoastHudLayout.MakeText(bg.rectTransform, "T", "", 12, TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));
            _text.horizontalOverflow = HorizontalWrapMode.Wrap; _text.verticalOverflow = VerticalWrapMode.Truncate;
            Application.logMessageReceived += OnLog;
        }

        void OnDestroy() { Application.logMessageReceived -= OnLog; }

        void OnLog(string msg, string stack, LogType type)
        {
            bool flowWarn = type == LogType.Warning && msg != null && (msg.StartsWith("[GameManager]") || msg.StartsWith("[SceneFlow]"));
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && !flowWarn) return;
            string first = stack != null ? stack.Split('\n')[0] : "";
            _lines.Add($"[{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} {Time.realtimeSinceStartup:0}s] {msg}  @ {first}");
            while (_lines.Count > 4) _lines.RemoveAt(0);
            if (_text != null) _text.text = string.Join("\n", _lines);
            _hideAt = Time.unscaledTime + 20f;
            if (_cg != null) _cg.alpha = 1f;
        }

        void Update()
        {
            if (_cg != null && _cg.alpha > 0f && Time.unscaledTime > _hideAt) _cg.alpha = 0f;
        }
    }
}
