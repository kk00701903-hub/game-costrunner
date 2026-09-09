using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 아케이드 진입 패널(타이틀·육성 공용 오버레이): 노을 달리기(무한) / 오늘의 런(조건 3·도장판) / 기록.
    public class ArcadeUI : MonoBehaviour
    {
        static ArcadeUI _active;
        public static bool IsOpen => _active != null;

        public static void Open(bool fromRaising = false)
        {
            if (_active != null) return;
            var go = new GameObject("ArcadeUI");
            DontDestroyOnLoad(go);
            _active = go.AddComponent<ArcadeUI>();
            _active._fromRaising = fromRaising;
            _active.Build();
        }

        bool _fromRaising;
        Canvas _canvas;
        RectTransform _root;
        SeasonKind _season = SeasonKind.Spring;
        Text _seasonLabel;

        static readonly Color Paper = new Color(0.98f, 0.95f, 0.88f, 0.96f);
        static readonly Color Ink = new Color(0.16f, 0.12f, 0.10f);

        void Build()
        {
            var gm = GameManager.Ensure();
            var p = gm.Profile; p.EnsureArrays();
            _canvas = CoastUiCanvas.Create("ArcadeCanvas", 455);
            DontDestroyOnLoad(_canvas.gameObject);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var bg = CoastHudLayout.MakeImage(_root, "Bg", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.08f, 0.06f, 0.09f, 0.97f));
            bg.raycastTarget = true;
            var tex = ArtAssets.LoadTexture("BG_TowerSunset");
            if (tex != null)
            {
                var art = CoastHudLayout.MakeImage(_root, "Art", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(1f, 1f, 1f, 0.28f));
                art.sprite = CoastUiArt.AsSprite(tex, 100f); art.raycastTarget = false;
            }

            var head = CoastOrnate.Label(_root, "Head", Loc.T("노을 달리기", "Sunset Run"), 30, CoastOrnate.Ivory);
            Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 44f));
            CoastUiArt.OutlineText(head, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            var sub = CoastOrnate.Label(_root, "Sub", Loc.T("이야기 밖에서, 그냥 달린다.", "Outside the story — just run."), 14, new Color(1f, 0.9f, 0.7f, 0.85f));
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(600f, 22f));

            // ── 무한 모드 카드 ──
            var c1 = Card(_root, "Endless", -420f, 250f);
            Title(c1, Loc.T("무한 달리기", "Endless"), Loc.T("끝없는 코스, 계속 빨라진다. 거리·코인·하트가 점수.", "An endless course that keeps speeding up. Distance, coins, hearts = score."));
            _seasonLabel = CoastOrnate.Label(c1, "Season", "", 18, Ink);
            Place(_seasonLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(400f, 30f));
            CoastOrnate.GlassButton(c1, "SeasonL", "◀", new Vector2(0.5f, 1f), new Vector2(-190f, -104f), new Vector2(60f, 34f), () => StepSeason(p, -1), 0.5f, 16, false);
            CoastOrnate.GlassButton(c1, "SeasonR", "▶", new Vector2(0.5f, 1f), new Vector2(190f, -104f), new Vector2(60f, 34f), () => StepSeason(p, 1), 0.5f, 16, false);
            var best1 = CoastOrnate.Label(c1, "Best", Loc.T($"최고 {p.endlessBestDist:N0}m · {p.endlessBestScore:N0}점 · 누적 {p.totalArcadeRuns}회", $"Best {p.endlessBestDist:N0} m · {p.endlessBestScore:N0} pts · {p.totalArcadeRuns} runs"), 14, new Color(0.4f, 0.35f, 0.32f));
            Place(best1.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(560f, 22f));
            CoastOrnate.GlassButton(c1, "Go", Loc.T("달리기", "Run"), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(260f, 48f), () => Begin(ArcadeKind.Endless), 0.5f, 20, true);
            RefreshSeason(p);

            // ── 오늘의 런 카드 ──
            bool done = ArcadeRun.DailyDoneToday(p);
            var c2 = Card(_root, "Daily", -700f, 300f);
            string date = DateTime.Now.ToString(Loc.IsKo ? "M월 d일" : "MMM d");
            var conds = ArcadeRun.MakeConditions(ArcadeRun.Today);
            var ds = ArcadeRun.DailySeason(ArcadeRun.Today, p);
            Title(c2, Loc.T($"오늘의 런  {date}", $"Daily Run  {date}"), Loc.T($"{ArcadeRun.SeasonName(ds)} 코스 · 오늘은 모두 같은 코스. 세 가지를 다 채우면 도장.", $"{ArcadeRun.SeasonName(ds)} course · same for everyone today. Clear all three for a stamp."));
            for (int i = 0; i < 3; i++)
            {
                var row = CoastOrnate.Label(c2, "C" + i, (done ? "✔  " : "○  ") + conds[i].Text, 17, done ? new Color(0.2f, 0.5f, 0.25f) : Ink, TextAnchor.MiddleLeft);
                Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(20f, -100f - i * 28f), new Vector2(480f, 26f));
            }
            var stamps = CoastOrnate.Label(c2, "Stamps", Loc.T($"도장 {p.DailyCount}개 · 연속 {p.dailyStreak}일 (최고 {p.dailyStreakBest}일)", $"{p.DailyCount} stamps · streak {p.dailyStreak} (best {p.dailyStreakBest})"), 14, new Color(0.4f, 0.35f, 0.32f));
            Place(stamps.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(560f, 22f));
            BuildStampRow(c2, p);
            CoastOrnate.GlassButton(c2, "Go", done ? Loc.T("오늘은 완료 — 한 번 더", "Done today — run again") : Loc.T("오늘의 런 시작", "Start daily run"), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 48f), () => Begin(ArcadeKind.Daily), 0.5f, 20, !done);

            // ── 하단 ──
            bool board = p.skateboardUnlocked && PlayerPrefs.GetInt("CoastRun_ArcadeBoard", 0) == 1;
            Button modeBtn = null;
            modeBtn = CoastOrnate.GlassButton(_root, "Mode", ModeLabel(p), new Vector2(0.5f, 0f), new Vector2(-150f, 60f), new Vector2(260f, 44f), () =>
            {
                if (!p.skateboardUnlocked) { CoastToast.Show(Loc.T("스케이트보드는 엔딩을 한 번 보면 열려요.", "The skateboard unlocks after one ending.")); return; }
                PlayerPrefs.SetInt("CoastRun_ArcadeBoard", PlayerPrefs.GetInt("CoastRun_ArcadeBoard", 0) == 1 ? 0 : 1);
                var t = modeBtn.GetComponentInChildren<Text>(); if (t != null) t.text = ModeLabel(p);
            }, 0.45f, 16, false);
            CoastOrnate.GlassButton(_root, "Close", Loc.T("닫기", "Close"), new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(260f, 44f), Close, 0.45f, 16, false);
        }

        static string ModeLabel(MetaProfile p)
        {
            bool board = p.skateboardUnlocked && PlayerPrefs.GetInt("CoastRun_ArcadeBoard", 0) == 1;
            return board ? Loc.T("🛹 스케이트보드", "🛹 Skateboard") : Loc.T("👟 러닝", "👟 Running");
        }

        void StepSeason(MetaProfile p, int dir)
        {
            for (int k = 0; k < 4; k++)
            {
                _season = (SeasonKind)(((int)_season + dir + 4) % 4);
                if (ArcadeRun.SeasonUnlocked(p, _season)) break;
            }
            RefreshSeason(p);
        }

        void RefreshSeason(MetaProfile p)
        {
            string s = ArcadeRun.SeasonName(_season);
            bool ok = ArcadeRun.SeasonUnlocked(p, _season);
            _seasonLabel.text = ok ? Loc.T($"계절: {s}", $"Season: {s}") : Loc.T($"계절: {s} (잠김)", $"Season: {s} (locked)");
        }

        void BuildStampRow(RectTransform parent, MetaProfile p)
        {
            // 최근 14일 도장 칸
            var set = new System.Collections.Generic.HashSet<int>(p.dailyStamps);
            for (int i = 13; i >= 0; i--)
            {
                int d = int.Parse(DateTime.Now.AddDays(-i).ToString("yyyyMMdd"));
                bool has = set.Contains(d);
                var cell = CoastUiArt.Panel(parent, "S" + i, has ? new Color(0.90f, 0.55f, 0.25f) : new Color(0.85f, 0.80f, 0.72f), 8);
                var rt = cell.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(-234f + (13 - i) * 36f, -230f);
                rt.sizeDelta = new Vector2(30f, 30f);
                if (has) { var t = CoastOrnate.Label(cell.transform, "T", "✔", 16, Color.white); CoastOrnate.Stretch(t.rectTransform, 0f, 0f, 0f, 0f); }
            }
        }

        RectTransform Card(RectTransform parent, string name, float y, float h)
        {
            var c = CoastUiArt.Panel(parent, name, Paper, 22);
            var rt = c.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(600f, h);
            return rt;
        }

        void Title(RectTransform card, string title, string desc)
        {
            var t = CoastOrnate.Label(card, "T", title, 24, Ink);
            Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(560f, 34f));
            var d = CoastOrnate.Label(card, "D", desc, 14, new Color(0.4f, 0.35f, 0.32f));
            Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(540f, 40f));
            d.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        void Begin(ArcadeKind kind)
        {
            var gm = GameManager.Ensure();
            bool fromRaising = _fromRaising;
            Close();
            ArcadeRun.Start(kind, gm, _season, fromRaising);
        }

        void Close()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
            _active = null;
        }

        void Update()
        {
            if (CoastRemoteKeys.Down(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)) Close();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (CoastRemoteKeys.Down(KeyCode.E)) Begin(ArcadeKind.Endless);
            if (Input.GetKeyDown(KeyCode.D)) Begin(ArcadeKind.Daily);
#endif
        }
    }

    /// 결과창 키 입력(Enter 다시 / Backspace·Esc 나가기 — 모바일 뒤로가기 포함).
    public class ArcadeResultKeys : MonoBehaviour
    {
        public Action retry, exit;
        void Update()
        {
            if (CoastRemoteKeys.Down(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) retry?.Invoke();
            else if (CoastRemoteKeys.Down(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)) exit?.Invoke();
        }
    }

    /// 아케이드 사망 결과창.
    public static class ArcadeResultUI
    {
        static GameObject _go;

        public static void Show(StageRunStats s, Action retry, Action exit)
        {
            Hide();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            var canvas = CoastUiCanvas.Create("ArcadeResult", 420);
            _go = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(0.1f, 0.03f, 0.08f, 0.8f));
            dim.raycastTarget = true;
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            bool daily = ArcadeRun.Kind == ArcadeKind.Daily;

            var panel = CoastUiArt.Panel(root, "Panel", new Color(0.98f, 0.95f, 0.88f, 1f), 28);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(560f, daily ? 620f : 500f);
            panel.raycastTarget = true;
            var ink = new Color(0.16f, 0.12f, 0.10f);

            float y = -30f;
            Label(prt, daily ? Loc.T("오늘의 런 결과", "Daily Run") : Loc.T("노을 달리기 결과", "Sunset Run"), 30, ink, y, 40f); y -= 50f;
            Label(prt, Loc.T($"{ArcadeRun.Distance:N0} m", $"{ArcadeRun.Distance:N0} m"), 54, new Color(0.9f, 0.45f, 0.2f), y, 60f); y -= 66f;
            Label(prt, Loc.T($"점수 {ArcadeRun.LastScore:N0}", $"Score {ArcadeRun.LastScore:N0}"), 24, ink, y, 32f); y -= 36f;
            if (s != null)
                Label(prt, Loc.T($"코인 {s.Coins} · 니어미스 {s.NearMissCount} · 최고 콤보 {s.BestCombo} · 하트 {s.Hearts}", $"Coins {s.Coins} · Near miss {s.NearMissCount} · Best combo {s.BestCombo} · Hearts {s.Hearts}"), 15, new Color(0.4f, 0.35f, 0.32f), y, 24f);
            y -= 34f;
            if (p != null)
            {
                bool nb = daily ? ArcadeRun.LastScore >= p.dailyBestScore : ArcadeRun.LastScore >= p.endlessBestScore;
                Label(prt, nb ? Loc.T("★ 최고 기록 갱신!", "★ New best!") : Loc.T($"최고 {(daily ? p.dailyBestScore : p.endlessBestScore):N0}점", $"Best {(daily ? p.dailyBestScore : p.endlessBestScore):N0}"), 16, nb ? new Color(0.85f, 0.5f, 0.1f) : new Color(0.4f, 0.35f, 0.32f), y, 24f);
                y -= 34f;
            }
            if (daily)
            {
                for (int i = 0; i < ArcadeRun.Conditions.Length && i < 3; i++)
                {
                    bool ok = ArcadeRun.ConditionDone[i];
                    Label(prt, (ok ? "✔  " : "○  ") + ArcadeRun.Conditions[i].Text, 18, ok ? new Color(0.2f, 0.5f, 0.25f) : ink, y, 26f, TextAnchor.MiddleLeft, 60f);
                    y -= 30f;
                }
                if (ArcadeRun.LastStamped) Label(prt, Loc.T($"오늘 도장 획득!  연속 {p?.dailyStreak ?? 0}일", $"Stamp earned!  Streak {p?.dailyStreak ?? 0}"), 18, new Color(0.9f, 0.45f, 0.2f), y - 4f, 28f);
                else if (p != null && ArcadeRun.DailyDoneToday(p)) Label(prt, Loc.T("오늘 도장은 이미 받았어요.", "Today's stamp is already yours."), 15, new Color(0.4f, 0.35f, 0.32f), y - 4f, 24f);
                y -= 40f;
            }

            Action doRetry = () => { Hide(); Time.timeScale = 1f; AudioListener.pause = false; retry?.Invoke(); };
            Action doExit = () => { Hide(); exit?.Invoke(); };
            Btn(prt, Loc.T("다시 달리기", "Run again"), new Color(0.30f, 0.72f, 0.36f), 96f, doRetry);
            Btn(prt, Loc.T("나가기", "Leave"), new Color(0.35f, 0.45f, 0.70f), 40f, doExit);
            var keys = _go.AddComponent<ArcadeResultKeys>(); keys.retry = doRetry; keys.exit = doExit;
            CoastAudioManager.PlayAnywhere(CoastSfx.Fail, 0.5f);
        }

        static void Label(RectTransform parent, string text, int size, Color color, float y, float h, TextAnchor align = TextAnchor.MiddleCenter, float inset = 20f)
        {
            var t = CoastOrnate.Label(parent, "L", text, size, color, align);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(-inset * 2f, h);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static void Btn(RectTransform parent, string text, Color color, float yFromBottom, Action act)
        {
            CoastOrnate.GlassButton(parent, "B", text, new Vector2(0.5f, 0f), new Vector2(0f, yFromBottom), new Vector2(320f, 46f), () => act?.Invoke(), 0.85f, 20, true);
        }

        public static void Hide()
        {
            if (_go != null) UnityEngine.Object.Destroy(_go);
            _go = null;
        }
    }
}
