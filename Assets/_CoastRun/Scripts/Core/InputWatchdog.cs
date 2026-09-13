using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 71차(사용자 「가끔 화면이 안 눌러진다」): 입력 감시견.
    ///   매 터치(마우스)마다 UI 레이캐스트를 해 보고, 맨 위에 걸린 것이 버튼(Selectable/클릭 핸들러)이 아닌 「막는 그림」이면 경고 로그를 남긴다.
    ///   4초 안에 헛터치가 3번 이어지면 → 화면 위에 작은 안내(어떤 오브젝트가 막았는지)를 4초 띄우고,
    ///   그 막는 그림이 **보이지 않는(알파 0.02 미만) 화면 크기 이미지**면 raycastTarget 을 꺼서 스스로 풀어 준다(잔존 딤·페이더 같은 것).
    ///   반투명 딤(팝업 뒤)은 의도된 것이라 건드리지 않는다. 로그 태그 [Input].
    public class InputWatchdog : MonoBehaviour
    {
        private static InputWatchdog _inst;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private readonly List<float> _deadTimes = new List<float>();
        private Text _note; private Canvas _noteCanvas; private float _noteUntil;
        private EventSystem _lastEs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_inst != null) return;
            var go = new GameObject("InputWatchdog");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<InputWatchdog>();
        }

        private void Update()
        {
            bool down = Input.GetMouseButtonDown(0);
            Vector2 pos = Input.mousePosition;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) { down = true; pos = Input.GetTouch(0).position; }
            if (_note != null && Time.unscaledTime > _noteUntil) { Destroy(_noteCanvas.gameObject); _note = null; _noteCanvas = null; }
            if (!down) return;
            var es = EventSystem.current;
            if (es == null) { Debug.LogWarning("[Input] EventSystem 없음 — UI 터치가 안 먹는다"); return; }
            if (es != _lastEs)
            {
                // 고밀도 폰(S25U ≈ 500 dpi)에서 기본 드래그 문턱 10px 은 손가락 떨림보다 작아 탭이 드래그로 바뀐다(스크롤 안 버튼이 안 눌림) → dpi 비례
                es.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(10f * Mathf.Max(96f, Screen.dpi) / 160f));
                _lastEs = es;
            }
            var ped = new PointerEventData(es) { position = pos };
            _hits.Clear();
            es.RaycastAll(ped, _hits);
            if (_hits.Count == 0) return;   // UI 밖(월드 탭) — 러닝 조작은 별도
            var top = _hits[0].gameObject;
            if (HasHandler(top)) return;
            // 맨 위가 핸들러 없는 그림 = 막힘. 크기·알파를 같이 기록.
            var g = top.GetComponent<Graphic>();
            var rt = top.transform as RectTransform;
            float cover = rt != null ? Mathf.Min(1f, rt.rect.width * rt.lossyScale.x / Mathf.Max(1f, Screen.width)) * Mathf.Min(1f, rt.rect.height * rt.lossyScale.y / Mathf.Max(1f, Screen.height)) : 0f;
            float alpha = g != null ? g.color.a * GroupAlpha(top.transform) : 1f;
            string path = Path(top.transform);
            Debug.LogWarning($"[Input] 헛터치 — 맨 위 {path} (화면 {cover:P0}, 알파 {alpha:0.00}, 핸들러 없음)");
            float now = Time.unscaledTime;
            _deadTimes.Add(now); _deadTimes.RemoveAll(t => now - t > 4f);
            if (_deadTimes.Count < 3) return;
            _deadTimes.Clear();
            bool healed = false;
            if (g != null && cover > 0.8f && alpha < 0.02f)
            {
                g.raycastTarget = false; healed = true;
                Debug.LogWarning($"[Input] 보이지 않는 전체 화면 그림이 터치를 막고 있어 raycastTarget 을 껐다: {path}");
            }
            ShowNote((healed ? "터치 막힘 해제: " : "터치 막힘: ") + top.name);
        }

        private static bool HasHandler(GameObject top)
        {
            for (var t = top.transform; t != null; t = t.parent)
            {
                if (t.GetComponent<Selectable>() != null) return true;
                if (t.GetComponent<IPointerClickHandler>() != null || t.GetComponent<IPointerDownHandler>() != null || t.GetComponent<IDragHandler>() != null) return true;
                if (t.GetComponent<Canvas>() != null) break;
            }
            return false;
        }

        private static float GroupAlpha(Transform t)
        {
            float a = 1f;
            for (; t != null; t = t.parent) { var cg = t.GetComponent<CanvasGroup>(); if (cg != null) { a *= cg.alpha; if (cg.ignoreParentGroups) break; } }
            return a;
        }

        private static string Path(Transform t)
        {
            string s = t.name; int n = 0;
            for (t = t.parent; t != null && n < 4; t = t.parent, n++) s = t.name + "/" + s;
            return s;
        }

        private void ShowNote(string msg)
        {
            if (_note == null)
            {
                var canvas = CoastUiCanvas.Create("InputNote", 900); _noteCanvas = canvas;
                DontDestroyOnLoad(canvas.gameObject);
                var root = CoastUiCanvas.Root(canvas);
                var bg = CoastHudLayout.MakeImage(root, "Bg", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -96f), new Vector2(-40f, -60f), new Color(0f, 0f, 0f, 0.55f));
                _note = CoastHudLayout.MakeText(bg.transform, "T", "", 12, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _note.color = new Color(1f, 0.9f, 0.5f);
            }
            _note.text = msg; _noteUntil = Time.unscaledTime + 4f;
        }
    }
}
