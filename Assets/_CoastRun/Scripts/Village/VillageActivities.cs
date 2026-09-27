using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 196차(사용자: 「중문관광지 놀거리 액티비티 — 요트·제트스키 / 서핑 / 승마·전동카트 / 테디베어 박물관·포토존」):
    /// 화면 위에 뜨는 짧은 미니게임(25~30초). 결과 점수로 코인·스트레스 해소.
    ///   Jetski · Kart = 좌우로 피하고 줍기 / Surf = 균형 잡기 / Horse = 타이밍 점프 / Yacht = 돌고래 사진 찍기
    public class JejuActivity : MonoBehaviour
    {
        public enum Kind { Jetski, Yacht, Surf, Kart, Horse }
        public static bool DevAuto;   // 개발용: 자동 플레이
        public static bool Playing => _cur != null;
        static JejuActivity _cur;

        Kind _kind; Action<int> _done; Canvas _cv; RectTransform _root, _field; Text _timeT, _scoreT, _hintT, _bigT;
        float _time, _dur; int _score; bool _running, _ended;

        public static void Play(Kind k, Action<int> onDone)
        {
            if (_cur != null) { onDone?.Invoke(0); return; }
            var go = new GameObject("JejuActivity_" + k); _cur = go.AddComponent<JejuActivity>();
            _cur._kind = k; _cur._done = onDone; _cur.Build();
        }
        public static string Title(Kind k) => k == Kind.Jetski ? Loc.T("제트스키", "Jet ski") : k == Kind.Yacht ? Loc.T("⛵ 요트 투어", "⛵ Yacht tour")
            : k == Kind.Surf ? Loc.T("색동해변 서핑", "Surfing") : k == Kind.Kart ? Loc.T("전동카트", "E-kart") : Loc.T("승마 체험", "Horse riding");
        static string Hint(Kind k) => k == Kind.Jetski ? Loc.T("누른 채 좌우로 — 노란 부표는 줍고, 바위는 피하기", "Hold & slide — grab buoys, dodge rocks")
            : k == Kind.Yacht ? Loc.T("돌고래가 뛰어오르면 톡! 사진을 찍자", "Tap the dolphins to take photos")
            : k == Kind.Surf ? Loc.T("기우는 반대쪽을 눌러 균형 잡기", "Press the opposite side to balance")
            : k == Kind.Kart ? Loc.T("누른 채 좌우로 — 코인은 줍고, 고깔은 피하기", "Hold & slide — coins yes, cones no")
            : Loc.T("표시가 초록 칸에 오면 톡! 울타리를 넘는다", "Tap when the marker hits green to jump");

        // ── 화면 ─────────────────────────────────────────────────────────
        void Build()
        {
            _cv = CoastUiCanvas.Create("JejuActivity", 172); _cv.transform.SetParent(transform, false);
            _root = CoastUiCanvas.Root(_cv);
            Color bg = _kind == Kind.Kart ? new Color(0.36f, 0.62f, 0.34f) : _kind == Kind.Horse ? new Color(0.62f, 0.80f, 0.45f) : _kind == Kind.Yacht ? new Color(0.98f, 0.66f, 0.45f) : new Color(0.25f, 0.60f, 0.88f);
            var dim = CoastHudLayout.MakeImage(_root, "Bg", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), bg); dim.raycastTarget = true;
            _field = CoastHudLayout.MakeImage(_root, "Field", new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero, FieldColor()).rectTransform;
            var title = CoastHudLayout.MakeText(_root, "Title", Title(_kind), 34, TextAnchor.MiddleCenter, new Vector2(0f, 0.905f), new Vector2(1f, 0.96f), Vector2.zero, Vector2.zero);
            title.color = Color.white; title.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(title, new Color(0f, 0f, 0f, 0.5f), 2f);
            _timeT = CoastHudLayout.MakeText(_root, "Time", "", 26, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.85f), new Vector2(0.5f, 0.9f), Vector2.zero, Vector2.zero); _timeT.color = Color.white; CoastUiArt.OutlineText(_timeT, new Color(0f, 0f, 0f, 0.5f), 2f);
            _scoreT = CoastHudLayout.MakeText(_root, "Score", "", 26, TextAnchor.MiddleRight, new Vector2(0.5f, 0.85f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero); _scoreT.color = Color.white; CoastUiArt.OutlineText(_scoreT, new Color(0f, 0f, 0f, 0.5f), 2f);
            _hintT = CoastHudLayout.MakeText(_root, "Hint", Hint(_kind), 20, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.12f), Vector2.zero, Vector2.zero);
            _hintT.color = Color.white; _hintT.horizontalOverflow = HorizontalWrapMode.Wrap; CoastUiArt.OutlineText(_hintT, new Color(0f, 0f, 0f, 0.5f), 2f);
            _bigT = CoastHudLayout.MakeText(_root, "Big", "", 64, TextAnchor.MiddleCenter, new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), Vector2.zero, Vector2.zero);
            _bigT.color = Color.white; _bigT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_bigT, new Color(0f, 0f, 0f, 0.6f), 3f);
            _dur = _kind == Kind.Horse ? 40f : _kind == Kind.Surf || _kind == Kind.Yacht ? 25f : 30f;
            BuildKind();
            StartCoroutine(Run());
        }
        Color FieldColor() => _kind == Kind.Kart ? new Color(0.42f, 0.42f, 0.46f) : _kind == Kind.Horse ? new Color(0.78f, 0.66f, 0.46f) : _kind == Kind.Yacht ? new Color(0.35f, 0.55f, 0.85f) : _kind == Kind.Surf ? new Color(0.30f, 0.70f, 0.90f) : new Color(0.18f, 0.48f, 0.80f);

        System.Collections.IEnumerator Run()
        {
            for (int i = 3; i > 0; i--) { _bigT.text = i.ToString(); yield return new WaitForSeconds(0.6f); }
            _bigT.text = Loc.T("시작!", "Go!"); yield return new WaitForSeconds(0.4f); _bigT.text = "";
            _running = true; _time = 0f;
            while (_time < _dur && !_ended) yield return null;
            _running = false;
            _bigT.text = Loc.T("끝!", "Finish!"); yield return new WaitForSeconds(0.9f); _bigT.text = "";
            // 결과 카드
            var card = CoastUiArt.CutePill(_root, "Result", new Color(1f, 0.97f, 0.92f), 28, 6); card.raycastTarget = true;
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(560f, 300f);
            var t = CoastHudLayout.MakeText(crt, "T", ResultText(), 28, TextAnchor.MiddleCenter, new Vector2(0f, 0.35f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-20f, -16f));
            t.color = EventCardKit.BrownInk; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            bool ok = false;
            var btn = CoastUiArt.GlossyPill(crt, "Ok", new Color(1f, 0.55f, 0.65f), 22, 8); btn.raycastTarget = true;
            var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.18f); brt.sizeDelta = new Vector2(240f, 70f);
            var bt = CoastHudLayout.MakeText(brt, "T", Loc.T("확인", "OK"), 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
            var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => { CoastPrefs.Vibrate(); ok = true; });
            float w = 0f; while (!ok) { w += Time.deltaTime; if (DevAuto && w > 1.2f) ok = true; yield return null; }
            var cb = _done; _cur = null; Destroy(gameObject); cb?.Invoke(_score);
        }
        string ResultText()
        {
            string unit = _kind == Kind.Yacht ? Loc.T("장", " photos") : _kind == Kind.Surf ? Loc.T("초", "s") : _kind == Kind.Horse ? Loc.T("번", " jumps") : Loc.T("개", "");
            string what = _kind == Kind.Yacht ? Loc.T("찍은 사진", "Photos") : _kind == Kind.Surf ? Loc.T("파도 위에 선 시간", "Time on the wave") : _kind == Kind.Horse ? Loc.T("넘은 울타리", "Fences cleared") : Loc.T("주운 것", "Collected");
            return $"{Title(_kind)}\n{what}: {_score}{unit}";
        }

        void Update()
        {
            if (!_running) { if (_timeT != null) _timeT.text = ""; return; }
            float dt = Time.deltaTime; _time += dt;
            _timeT.text = Loc.T($"⏱ {Mathf.CeilToInt(_dur - _time)}초", $"⏱ {Mathf.CeilToInt(_dur - _time)}s");
            _scoreT.text = _kind == Kind.Surf ? Loc.T($"{_score}초", $"{_score}s") : $"★ {_score}";
            switch (_kind)
            {
                case Kind.Jetski: case Kind.Kart: TickScroller(dt); break;
                case Kind.Surf: TickSurf(dt); break;
                case Kind.Horse: TickHorse(dt); break;
                case Kind.Yacht: TickYacht(dt); break;
            }
        }

        // 포인터(마우스·터치) — 필드 안 정규 좌표
        bool Pointer(out Vector2 n)
        {
            n = Vector2.zero;
            bool down = Input.GetMouseButton(0) || Input.touchCount > 0;
            if (!down) return false;
            Vector2 sp = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            var cam = _cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cv.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_field, sp, cam, out var lp)) return false;
            var r = _field.rect; n = new Vector2((lp.x - r.xMin) / r.width, (lp.y - r.yMin) / r.height); return true;
        }
        bool PointerDownThisFrame(out Vector2 n) { n = Vector2.zero; bool d = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began); return d && Pointer(out n); }

        Image Dot(string n, Color c, Vector2 size, RectTransform parent = null)
        {
            var i = CoastUiArt.GlossyPill(parent ?? _field, n, c, 12, 3); i.raycastTarget = false;
            var rt = i.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; return i;
        }
        void Place(RectTransform rt, Vector2 n) { rt.anchorMin = rt.anchorMax = n; rt.anchoredPosition = Vector2.zero; }
        void Pop(string s, Color c) { CoastToast.Pop(s); }

        // ── 준비물 ───────────────────────────────────────────────────────
        RectTransform _me; Text _meT; float _meX = 0.5f, _stun;
        readonly System.Collections.Generic.List<(RectTransform rt, Vector2 p, bool good)> _objs = new System.Collections.Generic.List<(RectTransform, Vector2, bool)>();
        readonly RectTransform[] _stripes = new RectTransform[8];
        float _spawnT, _angle, _angVel, _tq, _markT, _fenceT; int _fences; RectTransform _bar, _mark, _green, _board, _horse; Text _boardT;
        float _dolT; RectTransform _dol; float _dolLife;

        void BuildKind()
        {
            if (_kind == Kind.Jetski || _kind == Kind.Kart)
            {
                for (int i = 0; i < _stripes.Length; i++)
                {
                    var s = CoastHudLayout.MakeImage(_field, "Stripe", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, _kind == Kind.Kart ? new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 1f, 1f, 0.35f));
                    s.raycastTarget = false; _stripes[i] = s.rectTransform; _stripes[i].sizeDelta = _kind == Kind.Kart ? new Vector2(10f, 60f) : new Vector2(90f, 8f);
                }
                var me = Dot("Me", _kind == Kind.Kart ? new Color(0.95f, 0.30f, 0.35f) : new Color(1f, 0.55f, 0.20f), new Vector2(70f, 90f)); _me = me.rectTransform;
                _meT = CoastHudLayout.MakeText(_me, "T", "▲", 40, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else if (_kind == Kind.Surf)
            {
                for (int i = 0; i < 3; i++) { var w = CoastHudLayout.MakeImage(_field, "Wave", new Vector2(0f, 0.25f + i * 0.2f), new Vector2(1f, 0.28f + i * 0.2f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.3f)); w.raycastTarget = false; _stripes[i] = w.rectTransform; }
                var b = Dot("Board", new Color(1f, 0.85f, 0.35f), new Vector2(260f, 40f)); _board = b.rectTransform; Place(_board, new Vector2(0.5f, 0.42f));
                var person = Dot("Surfer", new Color(1f, 0.55f, 0.65f), new Vector2(60f, 110f), _board); person.rectTransform.anchoredPosition = new Vector2(0f, 72f);
                _boardT = CoastHudLayout.MakeText(_field, "Tilt", "", 22, TextAnchor.MiddleCenter, new Vector2(0f, 0.8f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero); _boardT.color = Color.white;
                var l = CoastHudLayout.MakeText(_field, "L", "◀", 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.05f), new Vector2(0.3f, 0.2f), Vector2.zero, Vector2.zero); l.color = new Color(1f, 1f, 1f, 0.6f);
                var r = CoastHudLayout.MakeText(_field, "R", "▶", 60, TextAnchor.MiddleCenter, new Vector2(0.7f, 0.05f), new Vector2(1f, 0.2f), Vector2.zero, Vector2.zero); r.color = new Color(1f, 1f, 1f, 0.6f);
            }
            else if (_kind == Kind.Horse)
            {
                var track = CoastHudLayout.MakeImage(_field, "Ground", new Vector2(0f, 0.3f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero, new Color(0.45f, 0.32f, 0.20f)); track.raycastTarget = false;
                var h = Dot("Horse", new Color(0.55f, 0.36f, 0.22f), new Vector2(120f, 80f)); _horse = h.rectTransform; Place(_horse, new Vector2(0.25f, 0.4f));
                CoastHudLayout.MakeText(_horse, "T", "♞", 44, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var bar = CoastHudLayout.MakeImage(_field, "Bar", new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.8f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.7f)); bar.raycastTarget = false; _bar = bar.rectTransform;
                var g = CoastHudLayout.MakeImage(_bar, "Green", new Vector2(0.62f, 0f), new Vector2(0.8f, 1f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.85f, 0.40f)); g.raycastTarget = false; _green = g.rectTransform;
                var m = CoastHudLayout.MakeImage(_bar, "Mark", new Vector2(0f, -0.4f), new Vector2(0f, 1.4f), new Vector2(-5f, 0f), new Vector2(5f, 0f), new Color(0.9f, 0.2f, 0.25f)); m.raycastTarget = false; _mark = m.rectTransform;
            }
            else if (_kind == Kind.Yacht)
            {
                var sun = Dot("Sun", new Color(1f, 0.85f, 0.45f), new Vector2(160f, 160f)); Place(sun.rectTransform, new Vector2(0.75f, 0.85f));
                var boat = Dot("Yacht", Color.white, new Vector2(200f, 70f)); Place(boat.rectTransform, new Vector2(0.5f, 0.12f));
                CoastHudLayout.MakeText(boat.rectTransform, "T", "⛵", 44, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                for (int i = 0; i < 3; i++) { var w = CoastHudLayout.MakeImage(_field, "Wave", new Vector2(0f, 0.3f + i * 0.15f), new Vector2(1f, 0.31f + i * 0.15f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.25f)); w.raycastTarget = false; _stripes[i] = w.rectTransform; }
            }
        }

        // 피하고 줍기(제트스키 · 카트)
        void TickScroller(float dt)
        {
            float speed = 0.45f + _time / _dur * 0.45f;
            for (int i = 0; i < _stripes.Length; i++)
            {
                if (_stripes[i] == null) continue;
                float y = Mathf.Repeat(i / (float)_stripes.Length - _time * speed, 1f);
                Place(_stripes[i], _kind == Kind.Kart ? new Vector2(i % 2 == 0 ? 0.33f : 0.66f, y) : new Vector2(0.15f + (i * 0.37f) % 0.7f, y));
            }
            // 조종
            if (_stun > 0f) _stun -= dt;
            else if (DevAuto)
            {
                float best = _meX, bd = 9f; foreach (var o in _objs) if (o.good && o.p.y > 0.1f && o.p.y < 0.6f && o.p.y < bd) { bd = o.p.y; best = o.p.x; }
                foreach (var o in _objs) if (!o.good && o.p.y > 0.08f && o.p.y < 0.3f && Mathf.Abs(o.p.x - best) < 0.1f) best += best > 0.5f ? -0.2f : 0.2f;
                _meX = Mathf.MoveTowards(_meX, Mathf.Clamp(best, 0.08f, 0.92f), dt * 1.6f);
            }
            else
            {
                if (Pointer(out var n)) _meX = Mathf.MoveTowards(_meX, Mathf.Clamp(n.x, 0.08f, 0.92f), dt * 2.2f);
                float kx = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1f : 0f);
                _meX = Mathf.Clamp(_meX + kx * dt * 1.4f, 0.08f, 0.92f);
            }
            Place(_me, new Vector2(_meX, 0.1f)); _me.localRotation = Quaternion.Euler(0f, 0f, _stun > 0f ? Mathf.Sin(_time * 40f) * 15f : 0f);
            // 생성
            _spawnT -= dt;
            if (_spawnT <= 0f)
            {
                _spawnT = Mathf.Lerp(0.55f, 0.32f, _time / _dur);
                bool good = UnityEngine.Random.value < 0.58f;
                var c = good ? (_kind == Kind.Kart ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.90f, 0.20f)) : (_kind == Kind.Kart ? new Color(1f, 0.50f, 0.15f) : new Color(0.30f, 0.30f, 0.34f));
                var d = Dot(good ? "Good" : "Bad", c, good ? new Vector2(46f, 46f) : new Vector2(64f, 56f));
                CoastHudLayout.MakeText(d.rectTransform, "T", good ? "★" : (_kind == Kind.Kart ? "▲" : "●"), 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _objs.Add((d.rectTransform, new Vector2(UnityEngine.Random.Range(0.1f, 0.9f), 1.05f), good));
            }
            for (int i = _objs.Count - 1; i >= 0; i--)
            {
                var o = _objs[i]; o.p.y -= speed * dt * 1.3f; _objs[i] = o; Place(o.rt, o.p);
                if (o.p.y < 0.16f && o.p.y > 0.04f && Mathf.Abs(o.p.x - _meX) < 0.085f)
                {
                    if (o.good) { _score++; CoastAudioManager.PlayAnywhere(CoastSfx.Coin); }
                    else { _score = Mathf.Max(0, _score - 1); _stun = 0.6f; CoastPrefs.Vibrate(); }
                    Destroy(o.rt.gameObject); _objs.RemoveAt(i); continue;
                }
                if (o.p.y < -0.06f) { Destroy(o.rt.gameObject); _objs.RemoveAt(i); }
            }
        }

        // 균형(서핑)
        float _surfAcc;
        void TickSurf(float dt)
        {
            for (int i = 0; i < 3; i++) if (_stripes[i] != null) _stripes[i].anchoredPosition = new Vector2(Mathf.Sin(_time * 1.5f + i) * 40f, Mathf.Sin(_time * 2f + i * 1.3f) * 14f);
            _tq = (Mathf.PerlinNoise(_time * 0.7f, 3.3f) - 0.5f) * 220f + Mathf.Sin(_time * 0.9f) * 30f;
            float push = 0f;
            if (DevAuto) push = -Mathf.Clamp(_angle / 12f + _angVel / 60f, -1f, 1f);
            else
            {
                if (Pointer(out var n)) push = n.x < 0.5f ? 1f : -1f;   // 왼쪽을 누르면 왼쪽으로 몸을 싣는다(시계 반대 = +)
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) push = 1f; if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) push = -1f;
            }
            _angVel += (_tq + push * 260f - _angle * 1.2f) * dt; _angVel *= 1f - dt * 1.4f; _angle += _angVel * dt;
            if (Mathf.Abs(_angle) > 60f)
            {
                _angle = 0f; _angVel = 0f; _surfAcc = 0f; _boardT.text = Loc.T("풍덩! 다시 일어나자", "Splash! Get back up"); CoastPrefs.Vibrate();
                _time += 2f;   // 넘어지면 2초 손해
            }
            else if (Mathf.Abs(_angle) < 30f) { _surfAcc += dt; if (_surfAcc >= 1f) { _surfAcc -= 1f; _score++; } _boardT.text = Loc.T("좋아, 파도를 탄다!", "Riding the wave!"); }
            else _boardT.text = Loc.T("기운다…!", "Tilting…!");
            if (_board != null) { _board.localRotation = Quaternion.Euler(0f, 0f, _angle); _board.anchoredPosition = new Vector2(Mathf.Sin(_time * 0.8f) * 60f, Mathf.Sin(_time * 2.2f) * 18f); }
        }

        // 타이밍(승마)
        float _markPos, _markSpd = 0.8f; float _jumpT;
        void TickHorse(float dt)
        {
            _markPos += _markSpd * dt; if (_markPos > 1f) { _markPos = 0f; _fences++; if (_fences >= 12) _ended = true; }
            if (_mark != null) { _mark.anchorMin = new Vector2(_markPos, -0.4f); _mark.anchorMax = new Vector2(_markPos, 1.4f); }
            bool tap = DevAuto ? (_markPos > 0.68f && _markPos < 0.72f && _jumpT <= 0f) : (PointerDownThisFrame(out var _) || Input.GetKeyDown(KeyCode.Space));
            if (tap && _jumpT <= 0f)
            {
                float g0 = _green.anchorMin.x, g1 = _green.anchorMax.x;
                if (_markPos >= g0 && _markPos <= g1) { _score++; _jumpT = 0.6f; CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _markSpd = Mathf.Min(1.6f, _markSpd + 0.06f); float w = Mathf.Max(0.1f, 0.18f - _score * 0.006f); float c0 = UnityEngine.Random.Range(0.45f, 0.85f - w); _green.anchorMin = new Vector2(c0, 0f); _green.anchorMax = new Vector2(c0 + w, 1f); }
                else { _jumpT = 0.4f; CoastPrefs.Vibrate(); }
                _markPos = 0f; _fences++; if (_fences >= 12) _ended = true;
            }
            if (_jumpT > 0f) _jumpT -= dt;
            if (_horse != null) _horse.anchoredPosition = new Vector2(0f, _jumpT > 0.2f ? Mathf.Sin((0.6f - _jumpT) / 0.4f * Mathf.PI) * 90f : Mathf.Abs(Mathf.Sin(_time * 9f)) * 10f);
        }

        // 사진(요트)
        void TickYacht(float dt)
        {
            for (int i = 0; i < 3; i++) if (_stripes[i] != null) _stripes[i].anchoredPosition = new Vector2(Mathf.Sin(_time * 0.6f + i * 2f) * 60f, 0f);
            if (_dol == null)
            {
                _dolT -= dt;
                if (_dolT <= 0f)
                {
                    var d = Dot("Dolphin", new Color(0.55f, 0.65f, 0.80f), new Vector2(110f, 70f)); _dol = d.rectTransform; _dolLife = UnityEngine.Random.Range(1.1f, 1.6f);
                    CoastHudLayout.MakeText(_dol, "T", "◆", 44, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Place(_dol, new Vector2(UnityEngine.Random.Range(0.15f, 0.85f), UnityEngine.Random.Range(0.3f, 0.7f)));
                }
            }
            else
            {
                _dolLife -= dt; _dol.anchoredPosition = new Vector2(0f, Mathf.Sin((1.6f - _dolLife) * 3f) * 40f);
                bool hit = false;
                if (DevAuto) hit = _dolLife < 0.8f;
                else if (PointerDownThisFrame(out var n)) { var a = _dol.anchorMin; hit = Mathf.Abs(n.x - a.x) < 0.14f && Mathf.Abs(n.y - a.y) < 0.12f; }
                if (hit) { _score++; CoastAudioManager.PlayAnywhere(CoastSfx.Coin); StartCoroutine(Flash()); Destroy(_dol.gameObject); _dol = null; _dolT = UnityEngine.Random.Range(0.4f, 1.2f); }
                else if (_dolLife <= 0f) { Destroy(_dol.gameObject); _dol = null; _dolT = UnityEngine.Random.Range(0.3f, 1f); }
            }
        }
        System.Collections.IEnumerator Flash()
        {
            var f = CoastHudLayout.MakeImage(_root, "Flash", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(1f, 1f, 1f, 0.8f)); f.raycastTarget = false;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime) { f.color = new Color(1f, 1f, 1f, 0.8f * (1f - t / 0.25f)); yield return null; }
            Destroy(f.gameObject);
        }
        void OnDestroy() { if (_cur == this) _cur = null; }
    }
}
