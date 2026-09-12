using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 49차(사용자): 구슬치기(48차-13/14) 수준으로 나머지 4종도 3D 무대 + 시안형 하단 조작 패널로 재작성.
    ///   공통 틀 Stage3DMission — 마당 0.29~0.92 = MiniStage3D(Kling 배경 UI_MG_Yard_<게임> + Blender 소품 MG_*), 발판 0~0.28 = 남색 패널(카드 + 큰 노란 버튼).
    ///   윷놀이: 멍석 위 윷가락 4개가 튀어 올라 돌다가 앉음(평면/등) · 말판 20칸 분필 원 · 3D 말(빨강 나 / 파랑 도담) 이동.
    ///   투호: 항아리(MG_Jar) · 화살(MG_TuhoArrow) 방향 반원 게이지 → 힘 게이지(흰 띠) → 포물선 비행 → 꽂힘/떨어짐.
    ///   딱지치기: 바닥 딱지(파랑) 위로 내 딱지(빨강)를 내리침 · 타이밍 게이지 · 넘어가면 뒤집혀 빨강.
    ///   무궁화: 운동장 레인 · 술래(집사 꼬마: 등/앞 그림 교체) · 나(하늘이 뒷모습) · 꾹 누르면 전진, 돌아보면 멈춤, 끝에서 [술래 터치!].
    public static partial class MissionMiniGames
    {
        private static readonly Color PanelNavy = new Color(0.16f, 0.22f, 0.42f);
        private static readonly Color PanelTitle = new Color(1f, 0.93f, 0.55f);
        private static readonly Color BtnYellow = new Color(1f, 0.80f, 0.20f);
        private static readonly Color BtnInk = new Color(0.45f, 0.18f, 0.02f);

        /// 3D 무대 미니게임 공통 틀.
        internal abstract class Stage3DMission : HomeMiniGames.MiniBase
        {
            protected MiniStage3D S;
            protected MiniKit Kit;              // 58차: 목표 띠·팝·탭 안내
            protected RectTransform BigRect;    // 58차: 큰 버튼(맥동·탭 안내용)
            protected abstract string Backdrop { get; }
            protected virtual float Pitch => 56f;
            protected virtual float Fov => 38f;

            /// 구슬치기와 같은 화면 비율: 마당 0.29~0.92, 발판 0~0.28(남색), 상태문은 마당 위 띠.
            protected void SetupStage(Transform foot)
            {
                var frame = Field.parent as RectTransform;
                if (frame != null) { frame.anchorMin = new Vector2(0f, 0.29f); frame.anchorMax = new Vector2(1f, 0.92f); }
                var footRt = foot as RectTransform;
                if (footRt != null) { footRt.anchorMin = new Vector2(0f, 0f); footRt.anchorMax = new Vector2(1f, 0.28f); }
                var footImg = foot.GetComponent<Image>(); if (footImg != null) footImg.color = new Color(0.12f, 0.16f, 0.34f);
                Rect(Status.rectTransform, new Vector2(0f, 0.83f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -2f));   // 56차-2: 두 줄까지
                // 56차-2: 캔버스 배율을 안 곱해 글자가 깨알만 했다 → Scaled(15) + 상자에 맞춰 줄어들기
                Status.fontSize = CoastHudLayout.Scaled(13); Status.fontStyle = FontStyle.Bold; Status.alignment = TextAnchor.MiddleCenter; Status.color = new Color(1f, 0.96f, 0.75f);
                Status.resizeTextForBestFit = true; Status.resizeTextMinSize = 10; Status.resizeTextMaxSize = CoastHudLayout.Scaled(13); Status.horizontalOverflow = HorizontalWrapMode.Wrap;
                CoastUiArt.OutlineText(Status, new Color(0f, 0f, 0f, 0.6f), 1.5f);
                S = MiniStage3D.Create(Field, Backdrop, Pitch, Fov, 10f);
                if (frame != null) Kit = MiniKit.Attach(frame, GetType().Name);
            }

            /// 남색 카드(제목 포함). x0~x1 은 발판 폭 비율.
            protected Image Card(Transform foot, string name, float x0, float x1, string title)
            {
                var card = CoastUiArt.CutePill(foot, name, PanelNavy, 18, 3);
                Rect(card.rectTransform, new Vector2(x0, 0.04f), new Vector2(x1, 0.80f), Vector2.zero, Vector2.zero); card.raycastTarget = false;
                if (!string.IsNullOrEmpty(title))
                {
                    var t = Txt(card.transform, "T", title, 17, PanelTitle, TextAnchor.UpperCenter);
                    Rect(t.rectTransform, new Vector2(0f, 0.76f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -6f)); t.fontStyle = FontStyle.Bold;
                    CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                }
                return card;
            }

            /// 큰 노란 버튼(발사!/던지기!). 라벨과 아래 화살표 글자를 돌려준다.
            protected Button BigButton(Transform foot, float x0, float x1, string label, Action onClick, out Text labelTxt, out Text arrowTxt, Color? color = null)
            {
                var fire = CoastUiArt.GlossyPill(foot, "Big", color ?? BtnYellow, 22, 10);
                Rect(fire.rectTransform, new Vector2(x0, 0.04f), new Vector2(x1, 0.80f), Vector2.zero, Vector2.zero); fire.raycastTarget = true;
                BigRect = fire.rectTransform; MiniKit.Pulse(BigRect, true);
                var fb = fire.gameObject.AddComponent<Button>(); fb.transition = Selectable.Transition.None;
                fb.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
                labelTxt = Txt(fire.transform, "T", label, 30, color.HasValue ? Color.white : BtnInk, TextAnchor.MiddleCenter);
                Rect(labelTxt.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero); labelTxt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(labelTxt, color.HasValue ? new Color(0f, 0f, 0f, 0.35f) : new Color(1f, 1f, 1f, 0.35f), 1.2f);
                arrowTxt = Txt(fire.transform, "Arrow", "→", 30, color.HasValue ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 0.55f, 0.1f), TextAnchor.MiddleCenter);
                Rect(arrowTxt.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.32f), Vector2.zero, Vector2.zero); arrowTxt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(arrowTxt, new Color(0.4f, 0.15f, 0f, 0.6f), 1.5f);
                return fb;
            }

            /// 세로 무지개 힘 게이지(초록→노랑→빨강). fill 은 anchorMax.y 로 0..1, band 는 목표 흰 띠(없으면 null).
            protected RectTransform VGauge(Transform card, float lo, float hi, out Text pctTxt, float? bandLo = null, float? bandHi = null)
            {
                var bar = CoastUiArt.CutePill(card, "Bar", new Color(0.06f, 0.08f, 0.18f), 12, 3);
                Rect(bar.rectTransform, new Vector2(0.40f, lo), new Vector2(0.62f, hi), Vector2.zero, Vector2.zero); bar.raycastTarget = false;
                var fillC = new GameObject("FillC", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                fillC.SetParent(bar.transform, false); Rect(fillC, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 4f), new Vector2(-4f, 0f));
                var segHost = new GameObject("Seg", typeof(RectTransform)).GetComponent<RectTransform>();
                segHost.SetParent(fillC, false); segHost.anchorMin = new Vector2(0f, 0f); segHost.anchorMax = new Vector2(1f, 0f); segHost.pivot = new Vector2(0.5f, 0f);
                segHost.anchoredPosition = Vector2.zero; segHost.sizeDelta = new Vector2(0f, 200f);
                var fit = segHost.gameObject.AddComponent<FitToParentHeight>(); fit.bar = bar.rectTransform; fit.pad = 8f;
                for (int i = 0; i < 8; i++)
                {
                    var seg = CoastHudLayout.MakeImage(segHost, "S" + i, new Vector2(0f, i / 8f), new Vector2(1f, (i + 1) / 8f), Vector2.zero, Vector2.zero,
                        Color.Lerp(Color.Lerp(new Color(0.2f, 0.9f, 0.4f), new Color(1f, 0.9f, 0.2f), Mathf.Clamp01(i / 4f)), new Color(1f, 0.25f, 0.2f), Mathf.Clamp01((i - 4) / 3.5f)));
                    seg.raycastTarget = false;
                }
                if (bandLo.HasValue && bandHi.HasValue)
                {
                    var band = CoastUiArt.Panel(bar.transform, "Band", new Color(1f, 1f, 1f, 0.55f), 3); band.raycastTarget = false;
                    Rect(band.rectTransform, new Vector2(-0.35f, bandLo.Value), new Vector2(1.35f, bandHi.Value), Vector2.zero, Vector2.zero);
                }
                string[] pct = { "0%", "50%", "100%" }; float[] py = { lo, (lo + hi) * 0.5f, hi };
                for (int i = 0; i < 3; i++)
                {
                    var l = Txt(card, "P" + i, pct[i], 11, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleRight);
                    Rect(l.rectTransform, new Vector2(0.02f, py[i] - 0.04f), new Vector2(0.37f, py[i] + 0.04f), Vector2.zero, Vector2.zero);
                }
                pctTxt = Txt(card, "Pct", "0%", 24, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
                Rect(pctTxt.rectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.28f), Vector2.zero, Vector2.zero); pctTxt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(pctTxt, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                return fillC;
            }

            /// 반원 방향 게이지(0/90/180 눈금 + 빨간 바늘). 바늘 회전은 z 각(90 = 정면).
            protected RectTransform HalfGauge(Transform card, float cy, out Text angleTxt)
            {
                var gaugeC = new GameObject("Gauge", typeof(RectTransform)).GetComponent<RectTransform>();
                gaugeC.SetParent(card, false); gaugeC.anchorMin = gaugeC.anchorMax = new Vector2(0.5f, cy); gaugeC.sizeDelta = Vector2.zero;
                for (int i = 0; i <= 24; i++)
                {
                    float a = Mathf.PI * (1f - i / 24f);
                    var p = CoastUiArt.Panel(gaugeC, "Arc" + i, i % 6 == 0 ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.45f, 0.75f, 1f, 0.9f), 4); p.raycastTarget = false;
                    p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    p.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * 62f, Mathf.Sin(a) * 62f);
                    p.rectTransform.sizeDelta = i % 6 == 0 ? new Vector2(9f, 9f) : new Vector2(6f, 6f);
                }
                string[] ticks = { "0", "90", "180" }; Vector2[] tp = { new Vector2(-62f, -14f), new Vector2(0f, 76f), new Vector2(62f, -14f) };
                for (int i = 0; i < 3; i++)
                {
                    var tt = Txt(gaugeC, "Tick" + i, ticks[i], 11, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
                    tt.rectTransform.anchorMin = tt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); tt.rectTransform.anchoredPosition = tp[i]; tt.rectTransform.sizeDelta = new Vector2(40f, 16f);
                }
                var hub = CoastUiArt.Panel(gaugeC, "Hub", new Color(0.95f, 0.3f, 0.3f), 7); hub.raycastTarget = false;
                hub.rectTransform.anchorMin = hub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); hub.rectTransform.sizeDelta = new Vector2(14f, 14f);
                var needle = CoastUiArt.Panel(gaugeC, "Needle", new Color(1f, 0.25f, 0.25f), 3); needle.raycastTarget = false;
                var n = needle.rectTransform; n.anchorMin = n.anchorMax = new Vector2(0.5f, 0.5f); n.pivot = new Vector2(0.5f, 0f); n.sizeDelta = new Vector2(6f, 58f);
                angleTxt = Txt(card, "Ang", "90°", 14, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
                Rect(angleTxt.rectTransform, new Vector2(0f, cy + 0.06f), new Vector2(1f, cy + 0.20f), Vector2.zero, Vector2.zero); angleTxt.fontStyle = FontStyle.Bold;
                return n;
            }

            /// 작은 힌트 글(카드 맨 아래)
            protected Text Hint(Transform card, string s)
            {
                var h = Txt(card, "Hint", s, 11, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
                Rect(h.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.20f), new Vector2(6f, 0f), new Vector2(-6f, 0f));
                return h;
            }

            /// 무대 위 그림 카드(알파 PNG). 카메라를 향해 세운 쿼드. 발이 바닥.
            protected Transform Sprite(string resName, float height, Color? tint = null)
            {
                var tex = ArtAssets.LoadTexture(resName);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Sprite_" + resName; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(S.Root, false);
                float w = tex != null ? height * tex.width / (float)tex.height : height * 0.6f;
                q.transform.localScale = new Vector3(w, height, 1f);
                var m = MiniStage3D.Lit(tint ?? Color.white, 0f, 0f, true);
                if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_SpecularHighlights")) { m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                return q.transform;
            }

            /// 카메라를 향하도록 세운 위치(발이 g). 살짝 카메라 쪽으로 기울여(빌보드) 위에서 봐도 납작해 보이지 않게.
            protected void Stand(Transform sprite, Vector3 g, float height)
            {
                var cam = S.Cam.transform;
                var fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
                var rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(-Pitch * 0.35f, 0f, 0f);
                sprite.rotation = rot;
                sprite.position = g + rot * new Vector3(0f, height * 0.5f, 0f);
            }

            protected static Color Wood => new Color(0.86f, 0.70f, 0.48f);
            protected static Color Bark => new Color(0.42f, 0.27f, 0.16f);

            /// 분필 원(바닥) — 흰 소프트 원판
            protected Transform Chalk(Vector2 n, float radius, Color c)
            {
                var g = S.GroundPoint(n);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Chalk"; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(S.Root, false);
                q.transform.position = g + Vector3.up * 0.006f; q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = MiniStage3D.SoftDisc(c); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                return q.transform;
            }

            protected static void Tint(GameObject go, Material m) { foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m; }
            protected static void Tint(GameObject go, string childContains, Material m) { foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.name.Contains(childContains)) r.sharedMaterial = m; }
        }

        /// 세그먼트 호스트 높이를 게이지 바 높이에 맞춘다(마스크 안에서 anchorMax 로 채움 비율을 조절).
        private class FitToParentWidth : MonoBehaviour
        {
            public RectTransform bar; public float pad;
            private void LateUpdate() { if (bar != null) { var rt = (RectTransform)transform; rt.sizeDelta = new Vector2(bar.rect.width - pad, rt.sizeDelta.y); } }
        }
        private class FitToParentHeight : MonoBehaviour
        {
            public RectTransform bar; public float pad;
            private void LateUpdate() { if (bar == null) return; var rt = (RectTransform)transform; rt.sizeDelta = new Vector2(0f, Mathf.Max(10f, bar.rect.height - pad)); }
        }

        // ══════════════════════════════════════════════════════════════════
        // 윷놀이 — 멍석 위 3D
        // ══════════════════════════════════════════════════════════════════
        private class YutMission3D : Stage3DMission
        {
            protected override string Title => Loc.T("미션 · 윷놀이", "Mission · Yut Nori");
            protected override string Backdrop => "UI_MG_Yard_Yut";
            private const int Cells = 20;
            private int _me, _ai;
            private bool _myTurn = true, _busy, _ended;
            private readonly List<Vector2> _cellPos = new List<Vector2>();
            private readonly Transform[] _sticks = new Transform[4];
            private readonly Material[] _stickMat = new Material[4];
            private Material _woodMat, _barkMat;
            private Transform _meTok, _aiTok, _meBlob, _aiBlob;
            private Text _resultBig, _resultSub, _meLbl, _aiLbl, _btnLabel, _btnArrow;
            private RectTransform _meBar, _aiBar;
            private float _stickLen, _tokH;

            protected override void Build(Transform foot)
            {
                SetupStage(foot);
                // 말판: 정사각 둘레 20칸(왼아래 출발, 시계 반대) — 분필 원, 모서리는 크고 진하게
                for (int i = 0; i <= Cells; i++) _cellPos.Add(CellAnchor(i));
                float wpn = S.WorldPerNorm(new Vector2(0.5f, 0.5f));
                var lineMat = MiniStage3D.Lit(new Color(1f, 1f, 1f, 0.85f), 0f);
                for (int i = 0; i < Cells; i++)
                {
                    bool corner = i % 5 == 0;
                    // 칸 사이 분필 선 + 칸(흰 원, 모서리는 노란 큰 원 + 갈색 점)
                    S.Bar(S.GroundPoint(_cellPos[i]), S.GroundPoint(_cellPos[i + 1]), wpn * 0.008f, 0.003f, lineMat);
                    Chalk(_cellPos[i], wpn * (corner ? 0.055f : 0.036f), corner ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(1f, 1f, 1f, 1f));
                    if (corner) Chalk(_cellPos[i], wpn * 0.026f, new Color(0.40f, 0.22f, 0.10f, 1f));
                }
                // 윷가락 4개 — 멍석 가운데 나란히
                _woodMat = MiniStage3D.Lit(Wood, 0.35f); _barkMat = MiniStage3D.Lit(Bark, 0.25f);
                _stickLen = wpn * 0.26f;
                for (int i = 0; i < 4; i++)
                {
                    var st = S.Spawn("MG_YutStick");
                    st.transform.localScale = Vector3.one * _stickLen;
                    _stickMat[i] = MiniStage3D.Lit(Wood, 0.35f);
                    Tint(st, _stickMat[i]);
                    _sticks[i] = st.transform;
                    RestStick(i, false);
                }
                // 말 두 개
                _tokH = wpn * 0.08f;
                _meTok = MakeToken(new Color(0.95f, 0.30f, 0.32f), out _meBlob);
                _aiTok = MakeToken(new Color(0.30f, 0.55f, 0.95f), out _aiBlob);
                PlaceToken(_meTok, _meBlob, 0, false); PlaceToken(_aiTok, _aiBlob, 0, true);

                // 발판: [결과 카드] [진행 카드] [던지기!]
                var res = Card(foot, "ResCard", 0.02f, 0.34f, Loc.T("윷 결과", "Throw"));
                _resultBig = Txt(res.transform, "Big", "—", 40, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
                Rect(_resultBig.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero); _resultBig.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(_resultBig, new Color(0f, 0f, 0f, 0.5f), 2f);
                _resultSub = Hint(res.transform, Loc.T("도1 · 개2 · 걸3 · 윷4 · 모5", "Do1 · Gae2 · Geol3 · Yut4 · Mo5"));
                var prog = Card(foot, "ProgCard", 0.36f, 0.60f, Loc.T("한 바퀴 경주", "Race"));
                _meBar = Track(prog.transform, 0.52f, new Color(0.95f, 0.30f, 0.32f), Loc.T("나", "Me"), out _meLbl);
                _aiBar = Track(prog.transform, 0.28f, new Color(0.30f, 0.55f, 0.95f), Loc.T("도담", "Dodam"), out _aiLbl);
                Hint(prog.transform, Loc.T("같은 칸 = 잡기!", "Same cell = catch!"));
                BigButton(foot, 0.62f, 0.98f, Loc.T("던지기!", "Throw!"), () => { if (_myTurn && !_busy && !_ended) StartCoroutine(Turn(true)); }, out _btnLabel, out _btnArrow);
                Status.text = Loc.T("내 차례 — [던지기!] 도담이보다 먼저 한 바퀴", "Your turn — [Throw!] Get around before Dodam");
                Kit?.Goal(Loc.T("도담이보다 먼저 한 바퀴!", "Get around before Dodam!")); Kit?.Score(Loc.T($"나 {_me}  ·  도담 {_ai}", $"Me {_me} · Dodam {_ai}")); Kit?.TapHint(BigRect, Loc.T("여기를 탭!", "Tap here!")); Kit?.Flash(Loc.T("준비 — 시작!", "Ready — Go!"));
            }

            private RectTransform Track(Transform card, float y, Color c, string label, out Text lbl)
            {
                lbl = Txt(card, "L", label, 12, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleLeft);
                Rect(lbl.rectTransform, new Vector2(0.06f, y + 0.06f), new Vector2(0.94f, y + 0.20f), Vector2.zero, Vector2.zero);
                var bg = CoastUiArt.Panel(card, "Bg", new Color(0.06f, 0.08f, 0.18f), 6); bg.raycastTarget = false;
                Rect(bg.rectTransform, new Vector2(0.06f, y - 0.05f), new Vector2(0.94f, y + 0.05f), Vector2.zero, Vector2.zero);
                var fill = CoastUiArt.Panel(bg.transform, "Fill", c, 5); fill.raycastTarget = false;
                Rect(fill.rectTransform, new Vector2(0f, 0f), new Vector2(0.02f, 1f), new Vector2(2f, 2f), new Vector2(0f, -2f));
                return fill.rectTransform;
            }

            private Transform MakeToken(Color c, out Transform blob)
            {
                var t = S.Spawn("MG_Token"); t.transform.localScale = Vector3.one * _tokH;
                var m = MiniStage3D.Lit(c, 0.55f); Tint(t, m);
                blob = S.Blob(_tokH * 0.6f, 0.45f).transform;
                return t.transform;
            }

            private void PlaceToken(Transform tok, Transform blob, int cell, bool ai)
            {
                var n = _cellPos[Mathf.Clamp(cell, 0, Cells)];
                if (ai) n += new Vector2(0.03f, -0.02f); else n += new Vector2(-0.02f, 0.015f);
                var g = S.GroundPoint(n);
                tok.position = g; blob.position = g + Vector3.up * 0.004f;
            }

            private static Vector2 CellAnchor(int i)
            {
                float l = 0.16f, r = 0.84f, b = 0.14f, t = 0.88f;
                i = Mathf.Clamp(i, 0, Cells);
                if (i <= 5) return new Vector2(Mathf.Lerp(l, r, i / 5f), b);
                if (i <= 10) return new Vector2(r, Mathf.Lerp(b, t, (i - 5) / 5f));
                if (i <= 15) return new Vector2(Mathf.Lerp(r, l, (i - 10) / 5f), t);
                return new Vector2(l, Mathf.Lerp(t, b, (i - 15) / 5f));
            }

            /// 가운데 나란히 눕힘. flat = 평평한 면(배)이 위.
            private void RestStick(int i, bool flat, float yawJitter = 0f)
            {
                var g = S.GroundPoint(new Vector2(0.38f + i * 0.08f, 0.50f));
                _sticks[i].position = g + Vector3.up * _stickLen * 0.11f;
                _sticks[i].rotation = Quaternion.Euler(0f, yawJitter, flat ? 180f : 0f);
                _stickMat[i].SetColor("_BaseColor", flat ? new Color(0.96f, 0.88f, 0.70f) : Bark);
            }

            private IEnumerator Turn(bool me)
            {
                _busy = true;
                _btnLabel.text = me ? Loc.T("던지는 중", "Throwing") : Loc.T("도담 차례", "Dodam");
                CoastAudioManager.PlayAnywhere(CoastSfx.Jump, 0.6f);
                bool[] flat = new bool[4]; int flats = 0;
                for (int i = 0; i < 4; i++) { flat[i] = UnityEngine.Random.value < 0.55f; if (flat[i]) flats++; }
                float[] spin = new float[4]; float[] jit = new float[4];
                for (int i = 0; i < 4; i++) { spin[i] = 540f + UnityEngine.Random.Range(0f, 360f); jit[i] = UnityEngine.Random.Range(-14f, 14f); }
                float dur = 0.85f, t = 0f;
                Vector3[] from = new Vector3[4];
                for (int i = 0; i < 4; i++) from[i] = _sticks[i].position;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / dur);
                    float h = Mathf.Sin(u * Mathf.PI) * _stickLen * 1.6f;
                    for (int i = 0; i < 4; i++)
                    {
                        var g = S.GroundPoint(new Vector2(0.38f + i * 0.08f, 0.50f + Mathf.Sin(u * Mathf.PI) * 0.04f));
                        _sticks[i].position = g + Vector3.up * (h + _stickLen * 0.11f);
                        float roll = spin[i] * u + (flat[i] ? 180f : 0f);
                        _sticks[i].rotation = Quaternion.Euler(Mathf.Sin(u * 9f + i) * 25f * (1f - u), jit[i] * u, roll);
                    }
                    yield return null;
                }
                for (int i = 0; i < 4; i++) RestStick(i, flat[i], jit[i]);
                CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit, 0.5f);
                int move; string name, en; bool again = false;
                switch (flats) { case 1: move = 1; name = "도"; en = "Do"; break; case 2: move = 2; name = "개"; en = "Gae"; break; case 3: move = 3; name = "걸"; en = "Geol"; break; case 4: move = 4; name = "윷"; en = "Yut"; again = true; break; default: move = 5; name = "모"; en = "Mo"; again = true; break; }
                _resultBig.text = Loc.T(name, en) + $" +{move}";
                _resultBig.color = again ? new Color(1f, 0.55f, 0.35f) : new Color(1f, 0.9f, 0.4f);
                if (me) Kit?.Pop(Loc.T(name, en) + $"  +{move}" + (again ? Loc.T("  한 번 더!", "  again!") : ""), true);
                _resultSub.text = (me ? Loc.T("나", "Me") : Loc.T("도담", "Dodam")) + (again ? Loc.T(" · 한 번 더!", " · again!") : "");
                yield return new WaitForSecondsRealtime(0.45f);
                for (int k = 0; k < move; k++)
                {
                    if (me) _me = Mathf.Min(Cells, _me + 1); else _ai = Mathf.Min(Cells, _ai + 1);
                    yield return Hop(me);
                }
                if (me && _me == _ai && _ai > 0 && _ai < Cells) { _ai = 0; PlaceToken(_aiTok, _aiBlob, 0, true); _resultSub.text = Loc.T("도담이를 잡았다! 한 번 더", "Caught Dodam! Again"); again = true; CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.7f); }
                if (!me && _ai == _me && _me > 0 && _me < Cells) { _me = 0; PlaceToken(_meTok, _meBlob, 0, false); _resultSub.text = Loc.T("잡혔다… 처음부터", "Caught… back to start"); again = true; CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.6f); }
                UpdateBars();
                yield return new WaitForSecondsRealtime(0.35f);
                Kit?.Score(Loc.T($"나 {Mathf.Min(_me, Cells)}  ·  도담 {Mathf.Min(_ai, Cells)}", $"Me {Mathf.Min(_me, Cells)} · Dodam {Mathf.Min(_ai, Cells)}"));
                if (_me >= Cells) { _ended = true; Kit?.Pop(Loc.T("승리!", "WIN!"), true); Status.text = Loc.T("먼저 들어왔다! 승리!", "Home first! Victory!"); _resultBig.text = Loc.T("승리!", "WIN!"); CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.8f); yield return new WaitForSecondsRealtime(1.0f); Finish(1); yield break; }
                if (_ai >= Cells) { _ended = true; Kit?.Pop(Loc.T("도담이가 먼저…", "Dodam first…"), false); Status.text = Loc.T("도담이가 먼저…", "Dodam got home first…"); _resultBig.text = Loc.T("패배…", "Lost…"); yield return new WaitForSecondsRealtime(1.0f); Finish(0); yield break; }
                _busy = false;
                if (again) { if (!me) StartCoroutine(Turn(false)); else { Status.text = Loc.T("한 번 더 던져!", "Throw again!"); _btnLabel.text = Loc.T("던지기!", "Throw!"); } yield break; }
                _myTurn = !me;
                if (_myTurn) { Status.text = Loc.T("내 차례 — [던지기!]", "Your turn — [Throw!]"); _btnLabel.text = Loc.T("던지기!", "Throw!"); }
                else { Status.text = Loc.T("도담이 차례…", "Dodam's turn…"); yield return new WaitForSecondsRealtime(0.5f); StartCoroutine(Turn(false)); }
            }

            private IEnumerator Hop(bool me)
            {
                var tok = me ? _meTok : _aiTok; var blob = me ? _meBlob : _aiBlob; int pos = me ? _me : _ai;
                var n0 = _cellPos[Mathf.Max(0, pos - 1)] + (me ? new Vector2(-0.02f, 0.015f) : new Vector2(0.03f, -0.02f));
                var n1 = _cellPos[pos] + (me ? new Vector2(-0.02f, 0.015f) : new Vector2(0.03f, -0.02f));
                float t = 0f, dur = 0.16f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / dur);
                    var g = S.GroundPoint(Vector2.Lerp(n0, n1, u));
                    tok.position = g + Vector3.up * Mathf.Sin(u * Mathf.PI) * _tokH * 1.2f;
                    blob.position = g + Vector3.up * 0.004f;
                    yield return null;
                }
                PlaceToken(tok, blob, pos, !me);
                UpdateBars();
            }

            private void UpdateBars()
            {
                _meBar.anchorMax = new Vector2(Mathf.Max(0.02f, _me / (float)Cells), 1f);
                _aiBar.anchorMax = new Vector2(Mathf.Max(0.02f, _ai / (float)Cells), 1f);
                _meLbl.text = Loc.T("나", "Me") + $"  {_me}/{Cells}";
                _aiLbl.text = Loc.T("도담", "Dodam") + $"  {_ai}/{Cells}";
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 투호 — 항아리 + 화살 포물선
        // ══════════════════════════════════════════════════════════════════
        private class TuhoMission3D : Stage3DMission
        {
            protected override string Title => Loc.T("미션 · 투호", "Mission · Tuho");
            protected override string Backdrop => "UI_MG_Yard_Tuho";
            protected override float Pitch => 38f;
            protected override float Fov => 40f;
            private enum Step { Aim, Power, Flying, Done }
            private Step _step = Step.Aim;
            private float _t, _angle, _power;
            private int _left = 5, _in;
            private const int Need = 3;
            // 배경 그림(한옥 마당)은 원근 그림이라 바닥이 아래 절반 — 소품은 n.y 0.45 아래에만.
            private static readonly Vector2 StartN = new Vector2(0.5f, 0.09f), JarN = new Vector2(0.5f, 0.30f);
            // 56차(사용자): 힘 게이지 10 % 느리게(2.2 → 1.98 Hz), 들어갈 확률 ↑(방향 ±6° → ±8°, 힘 띠 ±0.09 → ±0.12)
            private const float AimTol = 8f, PowerTol = 0.12f, NeedPower = 0.62f, PowerHz = 1.98f;
            private Transform _arrow, _jar, _arrowBlob;
            private float _arrowLen, _jarH;
            private RectTransform _needle, _powerFill;
            private Text _angleTxt, _powerTxt, _btnLabel, _btnArrow, _dirHint, _powHint;
            private readonly List<Image> _pips = new List<Image>();
            private Material _arrowMat;

            protected override void Build(Transform foot)
            {
                SetupStage(foot);
                float wpn = S.WorldPerNorm(new Vector2(0.5f, 0.5f));
                _jarH = wpn * 0.10f; _arrowLen = wpn * 0.115f;
                var jar = S.Spawn("MG_Jar"); _jar = jar.transform;
                jar.transform.localScale = Vector3.one * _jarH;
                Tint(jar, MiniStage3D.Lit(new Color(0.46f, 0.30f, 0.20f), 0.45f));
                Tint(jar, "Ear", MiniStage3D.Lit(new Color(0.32f, 0.20f, 0.13f), 0.4f));
                _jar.position = S.GroundPoint(JarN);
                S.Blob(_jarH * 0.75f, 0.5f).transform.position = S.GroundPoint(JarN) + Vector3.up * 0.004f;
                // 항아리 입 검은 원(위에서 보이게)
                var mouth = Chalk(JarN, _jarH * 0.30f, new Color(0.05f, 0.03f, 0.02f, 0.95f)); mouth.position = _jar.position + Vector3.up * _jarH * 1.0f;
                // 던지는 자리 분필 선
                Chalk(StartN, wpn * 0.05f, new Color(1f, 1f, 1f, 0.5f));
                _arrowMat = MiniStage3D.Lit(new Color(0.80f, 0.22f, 0.18f), 0.4f);
                _arrow = MakeArrow();
                _arrowBlob = S.Blob(_arrowLen * 0.25f, 0.35f).transform;

                // 발판: [방향] [힘] [버튼]
                var dir = Card(foot, "DirCard", 0.02f, 0.34f, Loc.T("방향 선택", "Direction"));
                _needle = HalfGauge(dir.transform, 0.30f, out _angleTxt);
                _dirHint = Hint(dir.transform, Loc.T("좌우로 움직이는 중…\n항아리를 향할 때!", "Sweeping…\nstop it on the jar!"));
                var pow = Card(foot, "PowCard", 0.36f, 0.60f, Loc.T("힘 선택", "Power"));
                _powerFill = VGauge(pow.transform, 0.30f, 0.74f, out _powerTxt, NeedPower - PowerTol, NeedPower + PowerTol);
                _powHint = Hint(pow.transform, Loc.T("흰 띠 안에서 멈춰!", "Stop inside the band!"));
                BigButton(foot, 0.62f, 0.98f, Loc.T("방향 확정", "Set aim"), OnButton, out _btnLabel, out _btnArrow);
                Kit?.Goal(Loc.T("5발 중 3발 항아리에!", "3 of 5 in the jar!")); Kit?.Pips(5, _left); Kit?.Score($"{_in} / {Need}"); Kit?.TapHint(BigRect, Loc.T("바늘이 항아리를 볼 때 탭!", "Tap when it points at the jar!")); Kit?.Flash(Loc.T("준비 — 시작!", "Ready — Go!"));
                ResetArrow();
                // 남은 화살 5개 핍(마당 위 상태띠 아래)
                for (int i = 0; i < 5; i++)
                {
                    var p = CoastUiArt.Panel(Field.parent, "Pip" + i, new Color(1f, 0.85f, 0.3f), 6); p.raycastTarget = false;
                    p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.02f); p.rectTransform.pivot = new Vector2(0.5f, 0f);
                    p.rectTransform.anchoredPosition = new Vector2((i - 2) * 22f, 8f); p.rectTransform.sizeDelta = new Vector2(14f, 28f);
                    _pips.Add(p);
                }
                Status.text = Loc.T($"남은 화살 {_left} · 넣은 것 {_in}/{Need} — 항아리를 향할 때 [방향 확정]", $"Arrows {_left} · in {_in}/{Need} — tap [Set aim] on the jar");
            }

            private Transform MakeArrow()
            {
                var a = S.Spawn("MG_TuhoArrow"); Tint(a, _arrowMat);
                Tint(a, "Fin", MiniStage3D.Lit(new Color(1f, 0.85f, 0.30f), 0.3f));
                a.transform.localScale = Vector3.one * _arrowLen;
                return a.transform;
            }

            private void ResetArrow()
            {
                _step = Step.Aim; _t = 0f; _power = 0f; _powerFill.anchorMax = new Vector2(1f, 0f); _powerTxt.text = "0%";
                _btnLabel.text = Loc.T("방향 확정", "Set aim");
                var g = S.GroundPoint(StartN);
                _arrow.position = g + Vector3.up * _arrowLen * 0.55f;
                _arrowBlob.position = g + Vector3.up * 0.004f;
            }

            private void OnButton()
            {
                if (_step == Step.Aim) { _step = Step.Power; _t = 0f; _btnLabel.text = Loc.T("발사!", "Throw!"); _angleTxt.text = $"{Mathf.RoundToInt(90f - _angle)}°"; Status.text = Loc.T("힘 게이지 — 흰 띠 안에서 [발사!]", "Power — tap [Throw!] inside the white band"); }
                else if (_step == Step.Power) StartCoroutine(Throw());
            }

            private void Update()
            {
                if (_arrow == null || S == null || _needle == null) return;
                if (_step == Step.Aim)
                {
                    _t += Time.unscaledDeltaTime; _angle = Mathf.Sin(_t * 1.5f) * 35f;
                    _needle.localRotation = Quaternion.Euler(0f, 0f, -_angle);
                    _angleTxt.text = $"{Mathf.RoundToInt(90f - _angle)}°";
                }
                else if (_step == Step.Power)
                {
                    _t += Time.unscaledDeltaTime; _power = 0.5f + 0.5f * Mathf.Sin(_t * PowerHz * Mathf.PI - Mathf.PI * 0.5f);
                    _powerFill.anchorMax = new Vector2(1f, _power); _powerTxt.text = $"{Mathf.RoundToInt(_power * 100f)}%";
                }
                if (_step == Step.Aim || _step == Step.Power)
                {
                    // 화살은 던지는 손에 들려 항아리 쪽으로 기울어 있음(방향각 반영)
                    var tip = Quaternion.Euler(-55f, _angle, 0f) * Vector3.forward;   // 촉이 앞·위로
                    _arrow.rotation = Quaternion.LookRotation(tip, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);   // FBX 촉 = 로컬 -Z
                }
            }

            private IEnumerator Throw()
            {
                _step = Step.Flying; _left--;
                if (_left < _pips.Count) _pips[_left].color = new Color(1f, 1f, 1f, 0.25f);
                CoastAudioManager.PlayAnywhere(CoastSfx.Jump, 0.6f);
                bool hit = Mathf.Abs(_angle) <= AimTol && Mathf.Abs(_power - NeedPower) <= PowerTol;
                var dirN = new Vector2(Mathf.Sin(_angle * Mathf.Deg2Rad), Mathf.Cos(_angle * Mathf.Deg2Rad));
                float dist = hit ? (JarN - StartN).magnitude : (JarN - StartN).magnitude * (_power / NeedPower);
                Vector2 endN = hit ? JarN : StartN + dirN * dist;
                endN.x = Mathf.Clamp(endN.x, 0.08f, 0.92f); endN.y = Mathf.Clamp(endN.y, 0.06f, 0.42f);   // 51차(사용자): 빗나가도 마당 바닥(그림의 모래 영역)에만 떨어진다
                Vector3 a = S.GroundPoint(StartN) + Vector3.up * _arrowLen * 0.55f;
                Vector3 b = hit ? _jar.position + Vector3.up * _jarH * 1.0f : S.GroundPoint(endN);
                float arc = _jarH * (hit ? 2.2f : 1.4f + _power);
                float t = 0f, dur = 0.55f;
                Vector3 prev = a;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / dur);
                    var p = Vector3.Lerp(a, b, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * arc;
                    var v = p - prev; if (v.sqrMagnitude > 1e-6f) _arrow.rotation = Quaternion.LookRotation(v.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
                    _arrow.position = p; prev = p;
                    _arrowBlob.position = S.GroundPoint(Vector2.Lerp(StartN, endN, u)) + Vector3.up * 0.004f;
                    yield return null;
                }
                if (hit)
                {
                    _in++; CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.7f);
                    // 꽂힌 채 남는 화살(살짝 기운 각)
                    var stuck = MakeArrow();
                    stuck.position = _jar.position + Vector3.up * _jarH * 0.55f + new Vector3(UnityEngine.Random.Range(-0.08f, 0.08f), 0f, 0f) * _jarH;
                    stuck.rotation = Quaternion.LookRotation(new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), -1f, UnityEngine.Random.Range(-0.15f, 0.25f)).normalized, Vector3.forward) * Quaternion.Euler(0f, 180f, 0f);
                    _arrowBlob.gameObject.SetActive(true);
                    StartCoroutine(Wobble(_jar));
                }
                else
                {
                    CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f);
                    // 바닥에 누움: 촉이 앞을 향한 채 납작하게 + 아래 그림자 원판(떠 있어 보이지 않게)
                    var g = S.GroundPoint(endN);
                    var lay = MakeArrow();
                    var flatDir = Quaternion.Euler(0f, _angle + UnityEngine.Random.Range(-25f, 25f), 0f) * Vector3.forward;
                    lay.rotation = Quaternion.LookRotation(flatDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
                    // 피벗이 화살 꼬리라 중심을 바닥 위 2cm 로: 꼬리 = 착지점 - 방향*길이/2
                    lay.position = g - flatDir * _arrowLen * 0.5f + Vector3.up * 0.015f;
                    var sh = S.Blob(_arrowLen * 0.22f, 0.4f).transform; sh.position = g + Vector3.up * 0.004f; sh.localScale = new Vector3(_arrowLen * 0.22f, _arrowLen * 0.9f, 1f);
                    sh.rotation = Quaternion.LookRotation(flatDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                }
                string why = hit ? "" : Mathf.Abs(_angle) > AimTol ? Loc.T("(방향이 빗나감)", "(off aim)") : _power < NeedPower ? Loc.T("(힘이 약함)", "(too weak)") : Loc.T("(힘이 셈)", "(too strong)");
                Kit?.Pop(hit ? Loc.T("쏙!", "IN!") : Loc.T("빗나감 " + why, "Miss " + why), hit); Kit?.Pips(5, _left); Kit?.Score($"{_in} / {Need}");
                Status.text = hit ? Loc.T($"쏙! 넣은 것 {_in}/{Need} · 남은 화살 {_left}", $"In! {_in}/{Need} · arrows {_left}")
                                  : Loc.T($"빗나감 {why} · 넣은 것 {_in}/{Need} · 남은 화살 {_left}", $"Miss {why} · {_in}/{Need} · arrows {_left}");
                yield return new WaitForSecondsRealtime(0.4f);
                if (_in >= Need) { _step = Step.Done; Status.text = Loc.T("3발 성공! 투호 명인", "3 in! Tuho master"); CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.8f); yield return new WaitForSecondsRealtime(0.8f); Finish(1); yield break; }
                if (_left <= 0) { _step = Step.Done; Status.text = Loc.T($"{_in}발… {Need}발이 필요해", $"{_in}… need {Need}"); yield return new WaitForSecondsRealtime(0.9f); Finish(0); yield break; }
                ResetArrow();
            }

            private IEnumerator Wobble(Transform tr)
            {
                float t = 0f; var rot = tr.rotation;
                while (t < 0.5f) { t += Time.unscaledDeltaTime; tr.rotation = rot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 6f * (1f - t * 2f)); yield return null; }
                tr.rotation = rot;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 딱지치기 — 골목 바닥
        // ══════════════════════════════════════════════════════════════════
        private class DdakjiMission3D : Stage3DMission
        {
            // 52차(사용자 시안): 한옥 골목 마당에 딱지 두 장이 **바닥에 납작하게**(왼쪽 파랑 구름·물결 = 도담이, 오른쪽 빨강 기하무늬 = 나),
            //   파란 딱지 위엔 **하늘색 표적 링**이 둥실 떠서 맥동. 발판은 [타이밍 가로 게이지 + 큰 % 알약] [남은 기회 N개 하트] [내리치기!].
            //   딱지 그림은 Kling(MG_Ddakji_Blue / MG_Ddakji_Red, 탑다운) — 없으면 색 타일.
            protected override string Title => Loc.T("미션 · 딱지치기", "Mission · Ddakji");
            protected override string Backdrop => "UI_MG_Yard_Ddakji";
            protected override float Pitch => 42f;
            private Transform _mine, _theirs, _theirsBlob, _mineBlob, _target;
            private Material _theirsM, _mineM, _targetM;
            private float _t; private const float Speed = 2.1f;
            private int _left = 3; private bool _busy, _ended;
            private const float ZoneL = 0.66f, ZoneR = 0.86f;
            private RectTransform _powerFill;
            private Text _powerTxt, _btnLabel, _btnArrow, _triesTxt, _triesTitle;
            private readonly List<Image> _hearts = new List<Image>();
            private float _size;
            private static readonly Vector2 TheirsN = new Vector2(0.40f, 0.33f);   // 골목 그림 바닥은 아래 60%
            private static readonly Vector2 MineN = new Vector2(0.66f, 0.28f);

            protected override void Build(Transform foot)
            {
                SetupStage(foot);
                float wpn = S.WorldPerNorm(new Vector2(0.5f, 0.5f));
                _size = wpn * 0.082f;
                _theirs = Tile("MG_Ddakji_Blue", new Color(0.22f, 0.40f, 0.85f), out _theirsM, out _theirsBlob);
                _mine = Tile("MG_Ddakji_Red", new Color(0.88f, 0.22f, 0.24f), out _mineM, out _mineBlob);
                Lay(_theirs, _theirsBlob, TheirsN, 8f);
                Lay(_mine, _mineBlob, MineN, -10f);
                // 표적 링(하늘색, 파란 딱지 위에 둥실)
                _target = TargetRing(out _targetM);

                // 발판: [타이밍 가로 게이지] [남은 기회] [내리치기!]
                var pow = Card(foot, "PowCard", 0.02f, 0.34f, Loc.T("타이밍", "Timing"));
                _powerFill = HGauge(pow.transform, out _powerTxt, ZoneL, ZoneR);
                var tries = Card(foot, "TryCard", 0.36f, 0.60f, "");
                _triesTitle = Txt(tries.transform, "TT", Loc.T("남은 기회 3개", "3 tries left"), 17, PanelTitle, TextAnchor.MiddleCenter);
                Rect(_triesTitle.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 0.46f), Vector2.zero, Vector2.zero); _triesTitle.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(_triesTitle, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                for (int i = 0; i < 3; i++)
                {
                    var h = new GameObject("H" + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    h.transform.SetParent(tries.transform, false); h.sprite = CoastUiArt.Icon("Heart"); h.preserveAspect = true; h.raycastTarget = false;
                    if (h.sprite == null) h.color = new Color(0.95f, 0.35f, 0.45f);
                    h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(0.5f, 0.66f); h.rectTransform.anchoredPosition = new Vector2((i - 1) * 36f, 0f); h.rectTransform.sizeDelta = new Vector2(32f, 32f);
                    _hearts.Add(h);
                }
                _triesTxt = Hint(tries.transform, Loc.T("3번 안에 한번 뒤집기", "Flip once in 3 tries"));
                BigButton(foot, 0.62f, 0.98f, Loc.T("내리치기!", "Slam!"), () => { if (!_busy && !_ended) StartCoroutine(Slam()); }, out _btnLabel, out _btnArrow);
                Kit?.Goal(Loc.T("3번 안에 한 번 뒤집기!", "Flip it once in 3 tries!")); Kit?.Pips(3, _left, new Color(1f, 0.45f, 0.45f)); Kit?.TapHint(BigRect, Loc.T("노란 구간에서 탭!", "Tap in the yellow zone!")); Kit?.Flash(Loc.T("준비 — 시작!", "Ready — Go!"));
                _btnArrow.text = "↓";
                Status.text = Loc.T($"노란 구간에서 내리쳐! 남은 기회 {_left}", $"Slam in the yellow zone! Tries {_left}");
            }

            /// 가로 무지개 타이밍 게이지(초록→노랑→빨강) + 노란 목표 띠 + 위 큰 % 알약(시안 「72%」).
            private RectTransform HGauge(Transform card, out Text pctTxt, float bandLo, float bandHi)
            {
                var bar = CoastUiArt.CutePill(card, "Bar", new Color(0.06f, 0.08f, 0.18f), 10, 3);
                Rect(bar.rectTransform, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.64f), Vector2.zero, Vector2.zero); bar.raycastTarget = false;
                var fillC = new GameObject("FillC", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                fillC.SetParent(bar.transform, false); Rect(fillC, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(4f, 4f), new Vector2(0f, -4f));
                var segHost = new GameObject("Seg", typeof(RectTransform)).GetComponent<RectTransform>();
                segHost.SetParent(fillC, false); segHost.anchorMin = new Vector2(0f, 0f); segHost.anchorMax = new Vector2(0f, 1f); segHost.pivot = new Vector2(0f, 0.5f);
                segHost.anchoredPosition = Vector2.zero; segHost.sizeDelta = new Vector2(400f, 0f);
                var fit = segHost.gameObject.AddComponent<FitToParentWidth>(); fit.bar = bar.rectTransform; fit.pad = 8f;
                for (int i = 0; i < 8; i++)
                {
                    var seg = CoastHudLayout.MakeImage(segHost, "S" + i, new Vector2(i / 8f, 0f), new Vector2((i + 1) / 8f, 1f), Vector2.zero, Vector2.zero,
                        Color.Lerp(Color.Lerp(new Color(0.2f, 0.9f, 0.4f), new Color(1f, 0.9f, 0.2f), Mathf.Clamp01(i / 4f)), new Color(1f, 0.25f, 0.2f), Mathf.Clamp01((i - 4) / 3.5f)));
                    seg.raycastTarget = false;
                }
                var band = CoastUiArt.Panel(bar.transform, "Band", new Color(1f, 0.85f, 0.2f, 0.6f), 3); band.raycastTarget = false;
                Rect(band.rectTransform, new Vector2(bandLo, -0.35f), new Vector2(bandHi, 1.35f), Vector2.zero, Vector2.zero);
                var l0 = Txt(card, "P0", "0%", 10, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft); Rect(l0.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.5f, 0.48f), Vector2.zero, Vector2.zero);
                var l1 = Txt(card, "P1", "100%", 10, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleRight); Rect(l1.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(0.92f, 0.48f), Vector2.zero, Vector2.zero);
                var pill = CoastUiArt.CutePill(card, "PctPill", new Color(0.30f, 0.80f, 0.95f), 12, 2); pill.raycastTarget = false;
                Rect(pill.rectTransform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.32f), Vector2.zero, Vector2.zero);
                pctTxt = Txt(pill.transform, "Pct", "0%", 22, new Color(0.05f, 0.15f, 0.30f), TextAnchor.MiddleCenter);
                Rect(pctTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 1f)); pctTxt.fontStyle = FontStyle.Bold;
                return fillC;
            }

            /// 바닥에 납작한 딱지 타일(탑다운 그림 쿼드). 그림이 없으면 색 타일.
            private Transform Tile(string res, Color fallback, out Material m, out Transform blob)
            {
                var tex = ArtAssets.LoadTexture(res);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Tile_" + res; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(S.Root, false);
                q.transform.localScale = new Vector3(_size * 2f, _size * 2f, 1f);
                m = MiniStage3D.Lit(tex != null ? Color.white : fallback, 0.25f, 0f, tex != null);
                if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                blob = S.Blob(_size * 0.95f, 0.35f).transform;
                return q.transform;
            }

            private void Lay(Transform tile, Transform blob, Vector2 n, float yaw)
            {
                var g = S.GroundPoint(n);
                tile.position = g + Vector3.up * 0.012f; tile.rotation = Quaternion.Euler(90f, yaw, 0f);
                blob.position = g + Vector3.up * 0.004f;
            }

            /// 하늘색 표적 링(동심원 3개 + 십자 눈금 + 가운데 점) — 절차적 텍스처, 파란 딱지 위 카메라 향해 둥실.
            private Transform TargetRing(out Material m)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "TargetRing"; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(S.Root, false);
                q.transform.localScale = new Vector3(_size * 4.2f, _size * 4.2f, 1f);
                m = MiniStage3D.Lit(new Color(0.45f, 0.95f, 1f, 0.9f), 0f, 0f, true);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", RingsTexture());
                if (m.HasProperty("_SpecularHighlights")) { m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                Stand(q.transform, S.GroundPoint(TheirsN), _size * 4.2f);
                return q.transform;
            }

            private static Texture2D _rings;
            private static Texture2D RingsTexture()
            {
                if (_rings != null) return _rings;
                const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TargetRings" };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 0f;
                    foreach (var rr in new[] { 0.92f, 0.64f, 0.36f }) a = Mathf.Max(a, Mathf.Clamp01((0.045f - Mathf.Abs(r - rr)) / 0.02f));
                    a = Mathf.Max(a, Mathf.Clamp01((0.09f - r) / 0.03f));   // 가운데 점
                    bool cross = (Mathf.Abs(dx) < 0.03f || Mathf.Abs(dy) < 0.03f) && r > 0.12f && r < 0.98f;
                    if (cross) a = Mathf.Max(a, 0.35f);
                    a = Mathf.Max(a, Mathf.Clamp01(1f - r / 0.98f) * 0.12f);   // 옅은 글로우
                    if (r > 0.99f) a = 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px); tex.Apply(false, true); _rings = tex; return tex;
            }

            private float Needle01 => 0.5f + 0.5f * Mathf.Sin(_t * Speed * Mathf.PI);

            private void Update()
            {
                if (S == null) return;
                // 표적 링 맥동(항상)
                if (_target != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.06f;
                    _target.localScale = new Vector3(_size * 4.2f * pulse, _size * 4.2f * pulse, 1f);
                    _target.position = S.GroundPoint(TheirsN) + Vector3.up * (_size * 2.4f + Mathf.Sin(Time.unscaledTime * 1.7f) * _size * 0.12f);
                    if (_targetM != null && _targetM.HasProperty("_BaseColor")) _targetM.SetColor("_BaseColor", new Color(0.45f, 0.95f, 1f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)));
                }
                if (_busy || _ended) return;
                _t += Time.unscaledDeltaTime;
                float v = Needle01;
                _powerFill.anchorMax = new Vector2(v, 1f); _powerTxt.text = $"{Mathf.RoundToInt(v * 100f)}%";
                // 내 딱지가 바닥에서 살짝 들썩(리듬)
                var g = S.GroundPoint(MineN);
                _mine.position = g + Vector3.up * (0.012f + v * _size * 0.12f);
            }

            private void SetTries()
            {
                if (_triesTitle != null) _triesTitle.text = Loc.T($"남은 기회 {_left}개", $"{_left} tries left");
                for (int i = 0; i < _hearts.Count; i++) _hearts[i].color = i < _left ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            }

            private IEnumerator Slam()
            {
                _busy = true; _left--;
                float v = Needle01;
                bool flip = v >= ZoneL && v <= ZoneR;
                SetTries();
                // 내 딱지를 번쩍 들었다가 파란 딱지 위로 내리친다
                Vector3 from = _mine.position; Quaternion fromR = _mine.rotation;
                Vector3 to = S.GroundPoint(TheirsN + new Vector2(0.02f, -0.02f)) + Vector3.up * _size * 0.10f;
                Vector3 apex = (from + to) * 0.5f + Vector3.up * _size * 2.2f;
                float t = 0f;
                while (t < 0.36f)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.36f);
                    float uu = u < 0.5f ? 1f - (1f - u * 2f) * (1f - u * 2f) : 1f;   // 들기(느리게) → 내리치기(빠르게)
                    Vector3 p = u < 0.5f ? Vector3.Lerp(from, apex, uu) : Vector3.Lerp(apex, to, (u - 0.5f) * 2f * (u - 0.5f) * 2f);
                    _mine.position = p; _mine.rotation = Quaternion.Slerp(fromR, Quaternion.Euler(90f, -8f, 0f), u);
                    _mineBlob.position = S.GroundPoint(Vector2.Lerp(MineN, TheirsN, u)) + Vector3.up * 0.004f;
                    yield return null;
                }
                CoastAudioManager.PlayAnywhere(flip ? CoastSfx.ChapterClear : CoastSfx.SoftHit, 0.7f);
                CoastPrefs.Vibrate();
                if (flip)
                {
                    // 상대 딱지가 붕 떠서 뒤집힌다 → 표적 링이 터지듯 커지며 사라짐
                    Vector3 g = S.GroundPoint(TheirsN); Quaternion r0 = _theirs.rotation;
                    t = 0f;
                    while (t < 0.6f)
                    {
                        t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.6f);
                        _theirs.position = g + Vector3.up * (0.012f + Mathf.Sin(u * Mathf.PI) * _size * 1.3f);
                        _theirs.rotation = r0 * Quaternion.Euler(0f, 0f, u * 180f);
                        _mine.position = to + new Vector3(0.6f, 0f, -0.4f) * _size * u;
                        if (_target != null) { _target.localScale = Vector3.one * _size * 4.2f * (1f + u * 1.5f); _targetM.SetColor("_BaseColor", new Color(0.45f, 0.95f, 1f, 1f - u)); }
                        yield return null;
                    }
                    if (_target != null) _target.gameObject.SetActive(false);
                    _theirs.rotation = r0 * Quaternion.Euler(0f, 0f, 180f); _theirs.position = g + Vector3.up * 0.012f;
                    _ended = true;
                    Status.text = Loc.T("넘어갔다! 내 딱지!", "Flipped! It's mine!"); Kit?.Pop(Loc.T("넘어갔다!", "FLIP!"), true);
                    _btnLabel.text = Loc.T("성공!", "Nice!");
                    yield return new WaitForSecondsRealtime(0.9f);
                    Finish(1); yield break;
                }
                // 실패: 상대 딱지는 들썩만, 내 딱지는 옆으로 튕겨 다시 제자리
                {
                    Vector3 g = S.GroundPoint(TheirsN); Quaternion r0 = _theirs.rotation;
                    Vector3 m0 = _mine.position;
                    t = 0f;
                    while (t < 0.35f)
                    {
                        t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.35f);
                        _theirs.position = g + Vector3.up * (0.012f + Mathf.Sin(u * Mathf.PI) * _size * 0.2f);
                        _theirs.rotation = r0 * Quaternion.Euler(Mathf.Sin(u * Mathf.PI) * 18f, 0f, 0f);
                        _mine.position = Vector3.Lerp(m0, S.GroundPoint(MineN) + Vector3.up * 0.012f, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * _size * 0.8f;
                        _mine.rotation = Quaternion.Euler(90f, -10f + Mathf.Sin(u * Mathf.PI) * 40f, 0f);
                        yield return null;
                    }
                    _theirs.rotation = r0; _theirs.position = g + Vector3.up * 0.012f;
                    Lay(_mine, _mineBlob, MineN, -10f);
                }
                Status.text = v < ZoneL ? Loc.T($"약해! 남은 기회 {_left}", $"Too weak! Tries {_left}") : Loc.T($"너무 세서 튕겼어! 남은 기회 {_left}", $"Too hard, it bounced! Tries {_left}");
                Kit?.Pop(v < ZoneL ? Loc.T("약해!", "Too weak!") : Loc.T("튕겼어!", "Bounced!"), false); Kit?.Pips(3, _left, new Color(1f, 0.45f, 0.45f));
                yield return new WaitForSecondsRealtime(0.35f);
                if (_left <= 0) { _ended = true; yield return new WaitForSecondsRealtime(0.5f); Finish(0); yield break; }
                _busy = false;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 무궁화 꽃이 피었습니다 — 운동장 레인
        // ══════════════════════════════════════════════════════════════════
        private class MugunghwaMission3D : Stage3DMission, IPointerDownHandler, IPointerUpHandler
        {
            protected override string Title => Loc.T("미션 · 무궁화 꽃이 피었습니다", "Mission · Red Light, Green Light");
            protected override string Backdrop => "UI_MG_Yard_Mugunghwa";
            protected override float Pitch => 40f;
            protected override float Fov => 42f;
            private Transform _me, _meBlob, _taggerFront, _taggerBack, _taggerBlob;
            private float _progress;
            private bool _holding, _turned, _ended, _touchable;
            private float _phaseT, _phaseLen;
            private int _caught;
            private Text _chant, _stateBig, _stateSub, _btnLabel, _btnArrow;
            private Image _stateCard, _runBtn;
            private RectTransform _progFill;
            private readonly List<Image> _lives = new List<Image>();
            private static readonly Vector2 MeStart = new Vector2(0.5f, 0.08f), MeEnd = new Vector2(0.5f, 0.44f), TaggerN = new Vector2(0.5f, 0.52f);   // 운동장 그림 지평선 ≈ 0.55
            private float _meH, _tagH, _bob;

            protected override void Build(Transform foot)
            {
                SetupStage(foot);
                float wpn = S.WorldPerNorm(new Vector2(0.5f, 0.45f));
                _meH = wpn * 0.20f; _tagH = S.WorldPerNorm(TaggerN) * 0.17f;
                // 레인 분필 두 줄
                var lmat = MiniStage3D.SoftDisc(new Color(1f, 1f, 1f, 0.55f));
                S.Bar(S.GroundPoint(new Vector2(0.40f, 0.04f)), S.GroundPoint(new Vector2(0.44f, 0.52f)), wpn * 0.012f, 0.004f, MiniStage3D.Lit(new Color(1f, 1f, 1f, 0.9f), 0f));
                S.Bar(S.GroundPoint(new Vector2(0.60f, 0.04f)), S.GroundPoint(new Vector2(0.56f, 0.52f)), wpn * 0.012f, 0.004f, MiniStage3D.Lit(new Color(1f, 1f, 1f, 0.9f), 0f));
                Chalk(MeStart, wpn * 0.10f, new Color(1f, 1f, 1f, 0.5f));
                Chalk(new Vector2(0.5f, 0.47f), wpn * 0.07f, new Color(1f, 0.85f, 0.3f, 0.6f));
                // 술래(앞/뒤 그림 두 장 교체) + 나(하늘이 뒷모습)
                _taggerFront = Sprite("UI_Butler_Boy", _tagH);
                _taggerBack = Sprite("MG_Char_ButlerBack", _tagH);
                if (_taggerBack.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap") == null) { Destroy(_taggerBack.gameObject); _taggerBack = null; }
                _taggerBlob = S.Blob(_tagH * 0.28f, 0.4f).transform;
                var tg = S.GroundPoint(TaggerN);
                Stand(_taggerFront, tg, _tagH); if (_taggerBack != null) Stand(_taggerBack, tg, _tagH);
                _taggerBlob.position = tg + Vector3.up * 0.004f;
                _me = Sprite("MG_Char_GirlBack", _meH);
                if (_me.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap") == null) { Destroy(_me.gameObject); _me = Sprite("GirlSkater_Back", _meH); }
                _meBlob = S.Blob(_meH * 0.26f, 0.4f).transform;
                _chant = Txt(Field.parent, "Chant", "", 22, new Color(1f, 1f, 1f), TextAnchor.MiddleCenter);
                // 58차: 목표 띠(마당 맨 위)와 겹치지 않게 구호는 띠 바로 아래, 안내(Status)는 그 아래
                Rect(_chant.rectTransform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.895f), Vector2.zero, Vector2.zero);
                _chant.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_chant, new Color(0.1f, 0.05f, 0.2f, 0.85f), 2f);
                _chant.resizeTextForBestFit = true; _chant.resizeTextMinSize = 12; _chant.resizeTextMaxSize = CoastHudLayout.Scaled(22);
                Status.rectTransform.anchorMin = new Vector2(0f, 0.70f); Status.rectTransform.anchorMax = new Vector2(1f, 0.80f);

                // 발판: [술래 상태] [진행도·목숨] [달리기 (꾹)]
                _stateCard = Card(foot, "StateCard", 0.02f, 0.34f, Loc.T("술래", "Tagger"));
                _stateBig = Txt(_stateCard.transform, "Big", "", 26, Color.white, TextAnchor.MiddleCenter);
                Rect(_stateBig.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero); _stateBig.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(_stateBig, new Color(0f, 0f, 0f, 0.5f), 2f);
                _stateSub = Hint(_stateCard.transform, "");
                var prog = Card(foot, "ProgCard", 0.36f, 0.60f, Loc.T("남은 거리", "Distance"));
                var bg = CoastUiArt.Panel(prog.transform, "Bg", new Color(0.06f, 0.08f, 0.18f), 6); bg.raycastTarget = false;
                Rect(bg.rectTransform, new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.62f), Vector2.zero, Vector2.zero);
                var fill = CoastUiArt.Panel(bg.transform, "Fill", new Color(0.35f, 0.9f, 0.5f), 5); fill.raycastTarget = false;
                Rect(fill.rectTransform, new Vector2(0f, 0f), new Vector2(0.01f, 1f), new Vector2(2f, 2f), new Vector2(0f, -2f)); _progFill = fill.rectTransform;
                for (int i = 0; i < 3; i++)
                {
                    var h = new GameObject("H" + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    h.transform.SetParent(prog.transform, false); h.sprite = CoastUiArt.Icon("Heart"); h.preserveAspect = true; h.raycastTarget = false;
                    if (h.sprite == null) h.color = new Color(0.95f, 0.35f, 0.45f);
                    h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(0.5f, 0.30f); h.rectTransform.anchoredPosition = new Vector2((i - 1) * 30f, 0f); h.rectTransform.sizeDelta = new Vector2(26f, 26f);
                    _lives.Add(h);
                }
                Hint(prog.transform, Loc.T("3번 걸리면 실패", "Caught 3 times = lose"));
                var b = BigButton(foot, 0.62f, 0.98f, Loc.T("달리기", "Run"), () => { if (_touchable && !_ended) StartCoroutine(Win()); }, out _btnLabel, out _btnArrow, new Color(0.93f, 0.22f, 0.52f));
                _btnArrow.text = Loc.T("(꾹 누르기)", "(hold)"); _btnArrow.fontSize = CoastHudLayout.Scaled(14);
                var hold = b.gameObject.AddComponent<HoldRelay>(); hold.target = this;
                NextPhase(false);
                _progress = 0f; PlaceMe();
                Status.text = Loc.T("[달리기]를 꾹 — 술래가 돌아보면 손을 떼! 끝까지 가면 [술래 터치!]", "Hold [Run] — let go when the tagger turns! Reach the end and [Tag!]");
                Kit?.Goal(Loc.T("술래에게 닿기! 돌아보면 멈춰", "Reach the tagger! Freeze when it turns")); Kit?.Pips(3, 3, new Color(1f, 0.45f, 0.45f)); Kit?.TapHint(BigRect, Loc.T("꾹 누르면 달려!", "Hold to run!")); Kit?.Flash(Loc.T("준비 — 시작!", "Ready — Go!"));
            }

            public void OnPointerDown(PointerEventData e) { _holding = true; }
            public void OnPointerUp(PointerEventData e) { _holding = false; }

            private void NextPhase(bool turned)
            {
                _turned = turned; _phaseT = 0f;
                _phaseLen = turned ? UnityEngine.Random.Range(0.9f, 1.6f) : UnityEngine.Random.Range(1.4f, 3.2f);
                if (_taggerBack != null) { _taggerBack.gameObject.SetActive(!turned); _taggerFront.gameObject.SetActive(turned); }
                else _taggerFront.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor", turned ? Color.white : new Color(0.55f, 0.55f, 0.62f));
                _chant.text = turned ? Loc.T("돌아봤다!", "Looking!") : Loc.T("무궁화 꽃이 피었습니다…", "Red light, green light…");
                _chant.color = turned ? new Color(1f, 0.35f, 0.35f) : Color.white;
                _stateBig.text = turned ? Loc.T("보고 있다!", "LOOKING!") : Loc.T("등 돌림", "Back turned");
                _stateBig.color = turned ? new Color(1f, 0.35f, 0.35f) : new Color(0.45f, 1f, 0.6f);
                _stateSub.text = turned ? Loc.T("멈춰!", "Freeze!") : Loc.T("지금 달려!", "Run now!");
                var fillImg = _stateCard.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null) fillImg.color = turned ? new Color(0.55f, 0.16f, 0.22f) : PanelNavy;
            }

            private void PlaceMe()
            {
                var n = Vector2.Lerp(MeStart, MeEnd, _progress);
                var g = S.GroundPoint(n);
                Stand(_me, g + Vector3.up * _bob, _meH);
                _meBlob.position = g + Vector3.up * 0.004f;
            }

            private void Update()
            {
                if (_ended || S == null) return;
                float dt = Time.unscaledDeltaTime;
                _phaseT += dt;
                if (_phaseT >= _phaseLen) NextPhase(!_turned);
                bool moving = false;
                if (_holding)
                {
                    if (_turned && _phaseT > 0.18f)
                    {
                        _caught++;
                        _progress = 0f; _holding = false;
                        CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.8f); CoastPrefs.Vibrate();
                        if (_caught - 1 < _lives.Count) _lives[3 - _caught].color = new Color(1f, 1f, 1f, 0.25f);
                        Status.text = Loc.T($"걸렸다! 처음부터 (걸린 횟수 {_caught}/3)", $"Caught! Back to start ({_caught}/3)"); Kit?.Pop(Loc.T("걸렸다!", "CAUGHT!"), false); Kit?.Pips(3, 3 - _caught, new Color(1f, 0.45f, 0.45f));
                        if (_caught >= 3) { _ended = true; _chant.text = Loc.T("아웃!", "OUT!"); StartCoroutine(EndAfter(0.9f, 0)); return; }
                    }
                    else { _progress = Mathf.Min(1f, _progress + dt * 0.22f); moving = true; }
                }
                _bob = moving ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f)) * _meH * 0.05f : 0f;
                PlaceMe();
                _progFill.anchorMax = new Vector2(Mathf.Max(0.01f, _progress), 1f);
                bool was = _touchable;
                _touchable = _progress >= 0.999f;
                if (_touchable)
                {
                    _btnLabel.text = Loc.T("술래 터치!", "Tag!"); _btnArrow.text = "★";
                    if (!_turned) _chant.text = Loc.T("지금! 술래를 터치!", "Now! Tap the tagger!");
                }
                else if (was) { _btnLabel.text = Loc.T("달리기", "Run"); _btnArrow.text = Loc.T("(꾹 누르기)", "(hold)"); }
            }

            private IEnumerator Win()
            {
                _ended = true;
                CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.8f);
                Status.text = Loc.T("술래 터치! 이겼다!", "Tagged! You win!"); Kit?.Pop(Loc.T("터치! 이겼다!", "TAG! WIN!"), true);
                _chant.text = Loc.T("만세!", "Hooray!"); _chant.color = new Color(1f, 0.9f, 0.4f);
                if (_taggerBack != null) { _taggerBack.gameObject.SetActive(false); _taggerFront.gameObject.SetActive(true); }
                float t = 0f;
                while (t < 0.9f) { t += Time.unscaledDeltaTime; _bob = Mathf.Abs(Mathf.Sin(t * 16f)) * _meH * 0.12f; PlaceMe(); yield return null; }
                Finish(1);
            }

            private IEnumerator EndAfter(float s, int r) { yield return new WaitForSecondsRealtime(s); Finish(r); }

            private class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
            {
                public MugunghwaMission3D target;
                public void OnPointerDown(PointerEventData e) => target?.OnPointerDown(e);
                public void OnPointerUp(PointerEventData e) => target?.OnPointerUp(e);
            }
        }
    }
}
