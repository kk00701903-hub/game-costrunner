using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 30차: 집 미니게임 3종. Start(kind, host, rewardable, onDone(rewardG)).
    ///   윷놀이(Yut)  — 윷 4개 던지기, 20칸 직선 트랙, 도담(AI)과 경주. 상대 말을 잡으면 출발로. 이기면 60G, 지면 0.
    ///   구슬치기(Marbles) — 끌어서 쏘기(방향·세기), 마당 밖으로 밀어낸 구슬 ×8G, 3발.
    ///   공기놀이(Gonggi) — 왕복하는 손이 노란 구간에 있을 때 [잡기], 5번, 성공 ×10G (전부 성공 +20G).
    public static class HomeMiniGames
    {
        public enum Kind { Yut, Marbles, Gonggi }

        private static readonly Color Navy = new Color(0.23f, 0.16f, 0.29f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.90f);
        private static readonly Color Coral = new Color(1f, 0.44f, 0.57f);
        private static readonly Color Mint = new Color(0.30f, 0.71f, 0.67f);
        private static readonly Color Sky = new Color(0.39f, 0.71f, 0.96f);
        private static readonly Color Sun = new Color(1f, 0.78f, 0.30f);
        private static readonly Color Grey = new Color(0.62f, 0.63f, 0.70f);

        public static void Start(Kind kind, RectTransform host, bool rewardable, Action<int> onDone)
        {
            var go = new GameObject("Game_" + kind, typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(CoastUiCanvas.HudPad + 6f, CoastUiCanvas.HudPad + 6f); rt.offsetMax = new Vector2(-CoastUiCanvas.HudPad - 6f, -CoastUiCanvas.HudPad - 6f);
            MiniBase g = kind == Kind.Yut ? go.AddComponent<YutGame>() : kind == Kind.Marbles ? go.AddComponent<MarbleGame>() : go.AddComponent<GonggiGame>();
            g.Init(rt, rewardable, onDone);
        }

        // ── 공용 ─────────────────────────────────────────────────────────

        public abstract class MiniBase : MonoBehaviour
        {
            protected RectTransform Root, Field;
            protected Text Status;
            protected bool Rewardable;
            private Action<int> _onDone;
            private bool _done;

            public void Init(RectTransform root, bool rewardable, Action<int> onDone)
            {
                Root = root; Rewardable = rewardable; _onDone = onDone;
                var head = CoastUiArt.CutePill(root, "Head", new Color(0.29f, 0.18f, 0.33f), 20, 4);
                Rect(head.rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                var t = Txt(head.transform, "Title", Title, 22, Cream, TextAnchor.MiddleLeft);
                Rect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), new Vector2(-150f, 0f));
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.4f), 1.5f);
                Btn(head.transform, "Quit", Loc.T("그만", "Quit"), Grey, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(110f, 44f), () => Finish(0));
                var frame = CoastUiArt.CutePill(root, "FieldFrame", new Color(0.95f, 0.88f, 0.72f), 22, 4);
                Rect(frame.rectTransform, new Vector2(0f, 0.16f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
                Field = new GameObject("Field", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                Field.SetParent(frame.transform, false);
                Rect(Field, Vector2.zero, Vector2.one, new Vector2(7f, 10f), new Vector2(-7f, -7f));
                var foot = CoastUiArt.CutePill(root, "Foot", Cream, 20, 4);
                Rect(foot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.15f), Vector2.zero, Vector2.zero);
                Status = Txt(foot.transform, "Status", "", 15, Navy, TextAnchor.MiddleLeft);
                Status.horizontalOverflow = HorizontalWrapMode.Wrap;
                Rect(Status.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 6f), new Vector2(-190f, -6f));
                Build(foot.transform);
                if (!rewardable) Status.text = Loc.T("연습 모드(보상 없음)", "Practice (no reward)");
            }

            protected abstract string Title { get; }
            protected abstract void Build(Transform foot);

            protected void Finish(int reward)
            {
                if (_done) return; _done = true;
                var cb = _onDone; _onDone = null;
                Destroy(gameObject);
                cb?.Invoke(reward);
            }

            protected static void Rect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
            { rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax; }

            protected static Text Txt(Transform parent, string name, string s, int size, Color color, TextAnchor align)
            { var t = CoastHudLayout.MakeText(parent, name, s, size, align, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); t.color = color; return t; }

            protected static Button Btn(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
            {
                var pill = CoastUiArt.CutePill(parent, name, color, 16, 3);
                pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = anchor; pill.rectTransform.pivot = anchor;
                pill.rectTransform.anchoredPosition = pos; pill.rectTransform.sizeDelta = size; pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
                var t = Txt(pill.transform, "T", label, 17, Color.white, TextAnchor.MiddleCenter);
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                return b;
            }

            protected static Image Dot(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
            {
                var im = CoastUiArt.Panel(parent, name, color, Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.5f));
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = anchor; im.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                im.rectTransform.anchoredPosition = Vector2.zero; im.rectTransform.sizeDelta = size;
                return im;
            }
        }

        // ── 윷놀이 ───────────────────────────────────────────────────────

        private class YutGame : MiniBase
        {
            protected override string Title => Loc.T("윷놀이", "Yut Nori");
            private const int Cells = 20;
            private int _me, _ai;            // 0 = 출발 전, Cells = 도착
            private bool _myTurn = true, _busy;
            private Image _meTok, _aiTok;
            private readonly Image[] _sticks = new Image[4];
            private Text _result;
            private Button _throwBtn;
            private readonly List<Vector2> _cellPos = new List<Vector2>();

            protected override void Build(Transform foot)
            {
                // 트랙: 네모 둘레 20칸(모서리 포함), 출발 = 왼쪽 아래
                for (int i = 0; i <= Cells; i++) _cellPos.Add(CellAnchor(i));
                for (int i = 0; i < Cells; i++)
                {
                    bool corner = i % 5 == 0;
                    var c = Dot(Field, "Cell" + i, _cellPos[i], corner ? new Vector2(34f, 34f) : new Vector2(22f, 22f), corner ? new Color(0.55f, 0.35f, 0.25f) : new Color(0.75f, 0.60f, 0.45f));
                    if (i == 0) Txt(c.transform, "S", Loc.T("출발", "Start"), 10, Cream, TextAnchor.MiddleCenter);
                }
                _meTok = Dot(Field, "Me", _cellPos[0], new Vector2(30f, 30f), Coral);
                Txt(_meTok.transform, "T", Loc.T("나", "Me"), 10, Color.white, TextAnchor.MiddleCenter);
                _aiTok = Dot(Field, "Ai", _cellPos[0], new Vector2(30f, 30f), Sky);
                _aiTok.rectTransform.anchoredPosition = new Vector2(10f, -8f);
                Txt(_aiTok.transform, "T", Loc.T("도담", "Dodam"), 9, Color.white, TextAnchor.MiddleCenter);
                // 윷 4개(가운데)
                for (int i = 0; i < 4; i++)
                {
                    var st = CoastUiArt.Panel(Field, "Stick" + i, new Color(0.85f, 0.70f, 0.50f), 8);
                    st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0.5f, 0.55f); st.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    st.rectTransform.anchoredPosition = new Vector2(-60f + i * 40f, 0f); st.rectTransform.sizeDelta = new Vector2(22f, 90f);
                    _sticks[i] = st;
                }
                _result = Txt(Field, "Res", Loc.T("[던지기]를 눌러 시작", "Press [Throw] to start"), 20, Navy, TextAnchor.MiddleCenter);
                Rect(_result.rectTransform, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.40f), Vector2.zero, Vector2.zero);
                _throwBtn = Btn(foot, "Throw", Loc.T("던지기", "Throw"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(160f, 54f), () => { if (_myTurn && !_busy) StartCoroutine(Turn(true)); });
                Status.text = Loc.T("내 차례. 도담이보다 먼저 한 바퀴!", "Your turn. Get around before Dodam!");
            }

            private static Vector2 CellAnchor(int i)
            {
                // 왼쪽 아래(0) → 오른쪽 아래(5) → 오른쪽 위(10) → 왼쪽 위(15) → 왼쪽 아래(20)
                float l = 0.10f, r = 0.90f, b = 0.10f, t = 0.90f;
                i = Mathf.Clamp(i, 0, Cells);
                if (i <= 5) return new Vector2(Mathf.Lerp(l, r, i / 5f), b);
                if (i <= 10) return new Vector2(r, Mathf.Lerp(b, t, (i - 5) / 5f));
                if (i <= 15) return new Vector2(Mathf.Lerp(r, l, (i - 10) / 5f), t);
                return new Vector2(l, Mathf.Lerp(t, b, (i - 15) / 5f));
            }

            private System.Collections.IEnumerator Turn(bool me)
            {
                _busy = true;
                // 던지기 연출: 0.6초 동안 뒤집기
                float t = 0f;
                bool[] flat = new bool[4];
                while (t < 0.6f)
                {
                    t += Time.unscaledDeltaTime;
                    for (int i = 0; i < 4; i++)
                    {
                        _sticks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f + i) * 25f);
                        _sticks[i].color = UnityEngine.Random.value > 0.5f ? new Color(0.85f, 0.70f, 0.50f) : new Color(0.45f, 0.30f, 0.20f);
                    }
                    yield return null;
                }
                int flats = 0;
                for (int i = 0; i < 4; i++) { flat[i] = UnityEngine.Random.value < 0.55f; if (flat[i]) flats++; _sticks[i].rectTransform.localRotation = Quaternion.identity; _sticks[i].color = flat[i] ? new Color(0.92f, 0.82f, 0.62f) : new Color(0.45f, 0.30f, 0.20f); }
                int move; string name; bool again = false;
                switch (flats) { case 1: move = 1; name = "도"; break; case 2: move = 2; name = "개"; break; case 3: move = 3; name = "걸"; break; case 4: move = 4; name = "윷"; again = true; break; default: move = 5; name = "모"; again = true; break; }
                _result.text = (me ? Loc.T("나: ", "Me: ") : Loc.T("도담: ", "Dodam: ")) + name + $" (+{move})" + (again ? Loc.T("  한 번 더!", "  again!") : "");
                yield return new WaitForSecondsRealtime(0.5f);
                // 이동(한 칸씩)
                for (int k = 0; k < move; k++)
                {
                    if (me) _me = Mathf.Min(Cells, _me + 1); else _ai = Mathf.Min(Cells, _ai + 1);
                    var tok = me ? _meTok : _aiTok; int pos = me ? _me : _ai;
                    tok.rectTransform.anchorMin = tok.rectTransform.anchorMax = _cellPos[pos];
                    tok.rectTransform.anchoredPosition = me ? Vector2.zero : new Vector2(10f, -8f);
                    tok.rectTransform.localScale = Vector3.one * 1.25f;
                    yield return new WaitForSecondsRealtime(0.12f);
                    tok.rectTransform.localScale = Vector3.one;
                }
                // 잡기
                if (me && _me == _ai && _ai > 0 && _ai < Cells) { _ai = 0; _aiTok.rectTransform.anchorMin = _aiTok.rectTransform.anchorMax = _cellPos[0]; _result.text += Loc.T("  도담이를 잡았다!", "  Caught Dodam!"); again = true; }
                if (!me && _ai == _me && _me > 0 && _me < Cells) { _me = 0; _meTok.rectTransform.anchorMin = _meTok.rectTransform.anchorMax = _cellPos[0]; _result.text += Loc.T("  잡혔다…", "  Caught…"); again = true; }
                yield return new WaitForSecondsRealtime(0.4f);
                if (_me >= Cells) { Status.text = Loc.T("이겼다! 60G", "You win! 60G"); _result.text = Loc.T("승리!", "Victory!"); yield return new WaitForSecondsRealtime(1.0f); Finish(60); yield break; }
                if (_ai >= Cells) { Status.text = Loc.T("도담이가 먼저 들어왔어. 다음에 또!", "Dodam got home first. Next time!"); _result.text = Loc.T("패배…", "Lost…"); yield return new WaitForSecondsRealtime(1.2f); Finish(0); yield break; }
                _busy = false;
                if (again) { if (!me) StartCoroutine(Turn(false)); else Status.text = Loc.T("한 번 더 던져!", "Throw again!"); yield break; }
                _myTurn = !me;
                if (_myTurn) Status.text = Loc.T("내 차례.", "Your turn.");
                else { Status.text = Loc.T("도담이 차례…", "Dodam's turn…"); yield return new WaitForSecondsRealtime(0.5f); StartCoroutine(Turn(false)); }
            }
        }

        // ── 구슬치기 ─────────────────────────────────────────────────────

        private class MarbleGame : MiniBase, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            protected override string Title => Loc.T("구슬치기", "Marbles");
            private class Marble { public RectTransform rt; public Vector2 pos, vel; public bool player, outOf; public float r = 18f; }
            private readonly List<Marble> _ms = new List<Marble>();
            private Marble _me;
            private int _shots = 3, _knocked;
            private Image _aim;
            private Vector2 _dragStart; private bool _aiming;
            private Image _ring;

            protected override void Build(Transform foot)
            {
                var ground = CoastHudLayout.MakeImage(Field, "Ground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.87f, 0.78f, 0.60f));
                ground.raycastTarget = true;   // 드래그 수신
                var yard = CoastUiArt.Panel(Field, "Yard", new Color(0.80f, 0.68f, 0.50f), 30);
                Rect(yard.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
                _ring = CoastUiArt.Panel(Field, "Ring", new Color(1f, 1f, 1f, 0.35f), 30);
                Rect(_ring.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.92f), new Vector2(4f, 4f), new Vector2(-4f, -4f));
                Color[] cols = { new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.75f, 0.95f), new Color(0.55f, 0.85f, 0.55f), new Color(0.95f, 0.85f, 0.40f), new Color(0.80f, 0.55f, 0.90f) };
                Vector2[] spots = { new Vector2(0.5f, 0.62f), new Vector2(0.38f, 0.72f), new Vector2(0.62f, 0.72f), new Vector2(0.44f, 0.82f), new Vector2(0.56f, 0.82f) };
                for (int i = 0; i < 5; i++) _ms.Add(Make(spots[i], cols[i], false));
                _me = Make(new Vector2(0.5f, 0.22f), Color.white, true);
                _aim = CoastHudLayout.MakeImage(Field, "Aim", new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.7f));
                _aim.rectTransform.pivot = new Vector2(0f, 0.5f); _aim.rectTransform.sizeDelta = new Vector2(0f, 4f); _aim.enabled = false;
                var drag = gameObject; // 이 컴포넌트 자체가 드래그 핸들러 — Field의 Ground 위에서 받도록 Root에 Image가 없으니 Ground에 위임
                var fwd = ground.gameObject.AddComponent<Forward>(); fwd.target = this;
                Status.text = Loc.T("흰 구슬을 끌어서 쏴. 남은 발: 3", "Drag the white marble to shoot. Shots left: 3");
            }

            private Marble Make(Vector2 anchor, Color c, bool player)
            {
                var im = Dot(Field, player ? "Me" : "Marble", anchor, new Vector2(36f, 36f), c);
                var gloss = CoastUiArt.Panel(im.transform, "Gloss", new Color(1f, 1f, 1f, 0.55f), 6);
                gloss.rectTransform.anchorMin = gloss.rectTransform.anchorMax = new Vector2(0.35f, 0.7f); gloss.rectTransform.sizeDelta = new Vector2(12f, 8f);
                var m = new Marble { rt = im.rectTransform, pos = anchor, player = player };
                return m;
            }

            private Vector2 Norm(Vector2 screen)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Field, screen, null, out var l);
                var r = Field.rect; return new Vector2((l.x - r.xMin) / r.width, (l.y - r.yMin) / r.height);
            }

            public void OnBeginDrag(PointerEventData e) { if (_shots <= 0 || Moving()) return; _aiming = true; _dragStart = Norm(e.position); _aim.enabled = true; }
            public void OnDrag(PointerEventData e)
            {
                if (!_aiming) return;
                var d = _dragStart - Norm(e.position);   // 당긴 반대 방향으로 쏜다(새총)
                float len = Mathf.Min(d.magnitude, 0.35f);
                _aim.rectTransform.anchorMin = _aim.rectTransform.anchorMax = _me.pos;
                _aim.rectTransform.sizeDelta = new Vector2(len * Field.rect.width * 1.2f, 4f);
                _aim.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            public void OnEndDrag(PointerEventData e)
            {
                if (!_aiming) return;
                _aiming = false; _aim.enabled = false;
                var d = _dragStart - Norm(e.position);
                float len = Mathf.Min(d.magnitude, 0.35f);
                if (len < 0.03f) return;
                _me.vel = d.normalized * len * 3.2f;
                _shots--;
                Status.text = Loc.T($"남은 발: {_shots}  ·  밀어낸 구슬: {_knocked}", $"Shots left: {_shots}  ·  knocked out: {_knocked}");
            }

            private bool Moving() { foreach (var m in _ms) if (m.vel.sqrMagnitude > 1e-5f) return true; return _me.vel.sqrMagnitude > 1e-5f; }

            private void Update()
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                var all = new List<Marble>(_ms) { _me };
                var r = Field.rect; float rad = 18f / r.width;   // 정규화 반지름(가로 기준)
                foreach (var m in all)
                {
                    if (m.outOf) continue;
                    m.pos += m.vel * dt;
                    m.vel *= Mathf.Pow(0.15f, dt);       // 마찰
                    if (m.vel.magnitude < 0.01f) m.vel = Vector2.zero;
                    // 흰 구슬은 마당 안에서 튕김, 색 구슬은 마당 밖으로 나가면 아웃
                    if (m.player)
                    {
                        if (m.pos.x < 0.08f + rad) { m.pos.x = 0.08f + rad; m.vel.x = -m.vel.x * 0.6f; }
                        if (m.pos.x > 0.92f - rad) { m.pos.x = 0.92f - rad; m.vel.x = -m.vel.x * 0.6f; }
                        if (m.pos.y < 0.10f + rad) { m.pos.y = 0.10f + rad; m.vel.y = -m.vel.y * 0.6f; }
                        if (m.pos.y > 0.92f - rad) { m.pos.y = 0.92f - rad; m.vel.y = -m.vel.y * 0.6f; }
                    }
                    else if (m.pos.x < 0.08f || m.pos.x > 0.92f || m.pos.y < 0.10f || m.pos.y > 0.92f)
                    {
                        m.outOf = true; m.vel = Vector2.zero; _knocked++;
                        m.rt.localScale = Vector3.one * 0.6f; var im = m.rt.GetComponent<Image>(); im.color = new Color(im.color.r, im.color.g, im.color.b, 0.35f);
                        Status.text = Loc.T($"남은 발: {_shots}  ·  밀어낸 구슬: {_knocked}", $"Shots left: {_shots}  ·  knocked out: {_knocked}");
                    }
                }
                // 충돌(원형, 같은 질량)
                for (int i = 0; i < all.Count; i++)
                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var a = all[i]; var b = all[j]; if (a.outOf || b.outOf) continue;
                        var d = b.pos - a.pos; d.y *= r.height / r.width;   // 화면 비율 보정
                        float dist = d.magnitude; float min = rad * 2f;
                        if (dist < min && dist > 1e-4f)
                        {
                            var n = d / dist; n.y *= r.width / r.height;
                            var rel = a.vel - b.vel; float p = Vector2.Dot(rel, n.normalized);
                            if (p > 0f) { a.vel -= n.normalized * p; b.vel += n.normalized * p; }
                            float push = (min - dist) * 0.5f;
                            a.pos -= n.normalized * push; b.pos += n.normalized * push;
                        }
                    }
                foreach (var m in all) { m.rt.anchorMin = m.rt.anchorMax = m.pos; }
                if (_shots <= 0 && !Moving())
                {
                    int reward = _knocked * 8;
                    Status.text = Loc.T($"끝! 밀어낸 구슬 {_knocked}개 → {reward}G", $"Done! {_knocked} knocked out → {reward}G");
                    _shots = -1;
                    StartCoroutine(EndAfter(1.2f, reward));
                }
            }

            private System.Collections.IEnumerator EndAfter(float s, int reward) { yield return new WaitForSecondsRealtime(s); Finish(reward); }

            /// Ground 이미지가 받은 드래그 이벤트를 게임으로 넘긴다.
            private class Forward : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                public MarbleGame target;
                public void OnBeginDrag(PointerEventData e) => target?.OnBeginDrag(e);
                public void OnDrag(PointerEventData e) => target?.OnDrag(e);
                public void OnEndDrag(PointerEventData e) => target?.OnEndDrag(e);
            }
        }

        // ── 공기놀이 ─────────────────────────────────────────────────────

        private class GonggiGame : MiniBase
        {
            protected override string Title => Loc.T("공기놀이", "Gonggi");
            private RectTransform _hand, _zone;
            private float _t, _speed = 1.1f, _zoneL = 0.42f, _zoneR = 0.58f;
            private int _round, _hits;
            private bool _waiting = true, _flash;
            private readonly List<Image> _stones = new List<Image>();
            private Text _big;

            protected override void Build(Transform foot)
            {
                var bg = CoastHudLayout.MakeImage(Field, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.98f, 0.94f, 0.85f));
                // 공깃돌 5개(위): 잡은 건 색이 채워진다
                for (int i = 0; i < 5; i++)
                {
                    var s = Dot(Field, "Stone" + i, new Vector2(0.30f + i * 0.10f, 0.80f), new Vector2(34f, 34f), new Color(0.85f, 0.85f, 0.88f));
                    _stones.Add(s);
                }
                _big = Txt(Field, "Big", Loc.T("1 / 5", "1 / 5"), 30, Navy, TextAnchor.MiddleCenter);
                Rect(_big.rectTransform, new Vector2(0.2f, 0.56f), new Vector2(0.8f, 0.70f), Vector2.zero, Vector2.zero);
                // 바 + 노란 구간 + 손
                var bar = CoastUiArt.Panel(Field, "Bar", new Color(0.75f, 0.65f, 0.55f), 10);
                Rect(bar.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero);
                var zone = CoastUiArt.Panel(Field, "Zone", Sun, 10);
                _zone = zone.rectTransform;
                var hand = CoastUiArt.CutePill(Field, "Hand", Coral, 14, 3);
                _hand = hand.rectTransform; _hand.anchorMin = _hand.anchorMax = new Vector2(0.08f, 0.40f); _hand.pivot = new Vector2(0.5f, 0.5f); _hand.sizeDelta = new Vector2(34f, 60f);
                Txt(_hand, "T", "✋", 14, Color.white, TextAnchor.MiddleCenter);
                Btn(foot, "Catch", Loc.T("잡기!", "Catch!"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(170f, 54f), Catch);
                NextRound();
                Status.text = Loc.T("손이 노란 구간에 있을 때 [잡기!]", "Tap [Catch!] while the hand is in the yellow zone");
            }

            private void NextRound()
            {
                _round++;
                if (_round > 5)
                {
                    int reward = _hits * 10 + (_hits == 5 ? 20 : 0);
                    _big.text = Loc.T($"성공 {_hits}/5 → {reward}G", $"{_hits}/5 → {reward}G");
                    Status.text = _hits == 5 ? Loc.T("다섯 개 다 잡았다! 보너스 +20G", "All five! Bonus +20G") : Loc.T("끝!", "Done!");
                    _waiting = false;
                    StartCoroutine(EndAfter(1.3f, reward));
                    return;
                }
                float w = Mathf.Lerp(0.18f, 0.09f, (_round - 1) / 4f);
                float c = UnityEngine.Random.Range(0.25f, 0.75f);
                _zoneL = c - w * 0.5f; _zoneR = c + w * 0.5f;
                _zone.anchorMin = new Vector2(Mathf.Lerp(0.08f, 0.92f, _zoneL), 0.36f); _zone.anchorMax = new Vector2(Mathf.Lerp(0.08f, 0.92f, _zoneR), 0.44f);
                _zone.offsetMin = _zone.offsetMax = Vector2.zero;
                _speed = 1.0f + (_round - 1) * 0.35f;
                _big.text = $"{_round} / 5";
                _waiting = true;
            }

            private void Catch()
            {
                if (!_waiting) return;
                float u = Mathf.PingPong(_t * _speed, 1f);
                bool hit = u >= _zoneL && u <= _zoneR;
                if (hit) { _hits++; _stones[_round - 1].color = Sun; Status.text = Loc.T("잡았다!", "Caught!"); }
                else { _stones[_round - 1].color = new Color(0.6f, 0.6f, 0.65f); Status.text = Loc.T("놓쳤다…", "Missed…"); }
                _waiting = false;
                StartCoroutine(Delay(0.5f, NextRound));
            }

            private void Update()
            {
                _t += Time.unscaledDeltaTime;
                float u = Mathf.PingPong(_t * _speed, 1f);
                _hand.anchorMin = _hand.anchorMax = new Vector2(Mathf.Lerp(0.08f, 0.92f, u), 0.40f);
            }

            private System.Collections.IEnumerator Delay(float s, Action a) { yield return new WaitForSecondsRealtime(s); a?.Invoke(); }
            private System.Collections.IEnumerator EndAfter(float s, int reward) { yield return new WaitForSecondsRealtime(s); Finish(reward); }
        }
    }
}
