using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 105차(재미요소 P1-3, 프린세스 메이커 수확제·스타듀 축제): 러닝이 없는 긴 구간에 **계절 축제 4번**.
    ///   형식은 기존 미니게임(ChapterMissionUI)을 그대로 쓰되, 그 축제의 **스탯이 등수에 보너스**로 들어간다 — 「키운 게 여기서 이긴다」.
    ///   등수 = 미니게임 승패 + 스탯 목표 달성: 둘 다 → 1등 / 승리만 → 2등 / 스탯만 → 3등 / 둘 다 아님 → 참가.
    ///   축제 주에는 격주 미니게임을 대신한다(총 횟수는 늘지 않음). 기록은 SaveData.festivalPlace[4](0 없음 / 1~3 등 / 4 참가).
    public static class Festival
    {
        public class Def
        {
            public int index;            // 0..3
            public int week;             // 열리는 주차(주말)
            public string ko, en;
            public StatKind stat;        // 보너스 스탯
            public int target;           // 이 값 이상이면 「스탯 달성」
            public ChapterMission.Kind game;
            public string art;           // Resources/CoastRun/UI_Fest_<n> (없으면 색 배너)
            public string Name => Loc.T(ko, en);
        }

        public static readonly Def[] All =
        {
            new Def { index = 0, week = 6,  ko = "유채꽃 사진대회",   en = "Canola Photo Contest", stat = StatKind.Charm,   target = 32, game = ChapterMission.Kind.Tuho,      art = "UI_Fest_Yuchae" },
            new Def { index = 1, week = 14, ko = "해녀 물질 대회",    en = "Haenyeo Dive Contest", stat = StatKind.Stamina, target = 52, game = ChapterMission.Kind.Mugunghwa, art = "UI_Fest_Haenyeo" },
            new Def { index = 2, week = 21, ko = "귤 따기 대회",      en = "Tangerine Picking",   stat = StatKind.Agility, target = 46, game = ChapterMission.Kind.Marbles,   art = "UI_Fest_Orange" },
            new Def { index = 3, week = 42, ko = "겨울 라디오 사연",  en = "Winter Radio Letters", stat = StatKind.Sense,   target = 40, game = ChapterMission.Kind.Yut,       art = "UI_Fest_Radio" },
        };

        public static Def AtWeek(int week) { foreach (var d in All) if (d.week == week) return d; return null; }
        public static Def Next(int week) { foreach (var d in All) if (d.week >= week) return d; return null; }
        public static int Place(SaveData s, int index) => s != null && s.festivalPlace != null && index < s.festivalPlace.Length ? s.festivalPlace[index] : 0;
        public static bool Pending(SaveData s) { var d = s != null ? AtWeek(s.week) : null; return d != null && Place(s, d.index) == 0; }

        public static string PlaceName(int place)
        {
            switch (place)
            {
                case 1: return Loc.T("1등", "1st");
                case 2: return Loc.T("2등", "2nd");
                case 3: return Loc.T("3등", "3rd");
                case 4: return Loc.T("참가", "Entered");
                default: return "";
            }
        }
        public static int Money(int place) => place == 1 ? 300 : place == 2 ? 200 : place == 3 ? 100 : 50;
        public static int Hearts(int place) => place == 1 ? 3 : place == 2 ? 2 : place == 3 ? 1 : 0;

        /// 결과 판정 + 보상 적용. 돌려주는 값 = 등수.
        public static int Settle(GameManager gm, Def d, bool won)
        {
            var s = gm != null ? gm.Save : null; if (s == null || d == null) return 0;
            bool statOk = s.stats.Get(d.stat) >= d.target;
            int place = won && statOk ? 1 : won ? 2 : statOk ? 3 : 4;
            if (s.festivalPlace == null || s.festivalPlace.Length < All.Length) s.festivalPlace = new int[All.Length];
            s.festivalPlace[d.index] = place;
            s.stats.money += Money(place);
            s.chapterHearts += Hearts(place);
            s.stats.hearts += Hearts(place);
            LevelSystem.Add(place <= 2 ? LevelSystem.ExpMinigame : LevelSystem.ExpMinigame / 2);
            if (place == 1)
            {
                var drop = RoomDeco.TryDropFromRun(gm.Profile, s.seed * 977 + d.week * 31 + 3, 0.9f);
                if (drop != null) CoastToast.Show(Loc.T($"축제 상품 — 「{drop.Name}」!", $"Festival prize — \"{drop.Name}\"!"));
            }
            gm.Persist();
            return place;
        }
    }

    /// 축제 안내·결과 카드(EventCardKit 크림 카드).
    public static class FestivalUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void ShowIntro(Festival.Def d, SaveData s, Action onGo)
        {
            Close();
            if (d == null || s == null) { onGo?.Invoke(); return; }
            var crt = EventCardKit.Card("FestivalIntroCanvas", 466, new Vector2(640f, 760f), out _canvas, 20f);
            Banner(crt, d, 24f, 150f);
            EventCardKit.Kid(crt, Loc.T("누나, 나가 보자!", "Let's enter!"), true);   // 109차: 꼬마 동행
            var kicker = CoastHudLayout.MakeText(crt, "K", Loc.T($"{Timeline.SeasonName(Timeline.SeasonOf(d.week))} 축제 · {d.week}주차", $"{Timeline.SeasonName(Timeline.SeasonOf(d.week))} festival · week {d.week}"), 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -212f), new Vector2(0f, -180f));
            kicker.color = new Color(0.90f, 0.32f, 0.45f); kicker.fontStyle = FontStyle.Bold;
            EventCardKit.JellyTitle(crt, d.Name, new Color(0.98f, 0.60f, 0.20f), new Color(0.50f, 0.22f, 0.05f), 214f, 84f, 40);
            EventCardKit.Divider(crt, 306f);
            var g = ChapterMission.Get(d.game);
            int have = s.stats.Get(d.stat);
            EventCardKit.IconRow(crt, "Icon_Bang", new Color(1f, 0.85f, 0.45f), Loc.T($"종목 · {g.nameKo}", $"Game · {g.nameEn}"), 330f, 60f, 22);
            EventCardKit.IconRow(crt, "Icon_Star", new Color(0.75f, 0.62f, 1f), Loc.T($"{RaisingFun.StatName(d.stat)} {have} / 목표 {d.target}", $"{RaisingFun.StatName(d.stat)} {have} / target {d.target}") + (have >= d.target ? " ✓" : ""), 398f, 60f, 22);
            var box = EventCardKit.InfoBox(crt, 476f, 150f);
            EventCardKit.IconRow(box, "Icon_Bulb", new Color(0.80f, 0.88f, 1f), Loc.T("이기고 스탯도 채우면 1등 · 하나만 되면 2·3등", "Win + stat = 1st · one of them = 2nd/3rd"), 14f, 52f, 18, null, null, 18f, 14f);
            EventCardKit.IconRow(box, "Icon_Coin", new Color(1f, 0.85f, 0.45f), Loc.T("1등 300G ♥3 상품 · 2등 200G ♥2 · 3등 100G ♥1", "1st 300G ♥3 prize · 2nd 200G ♥2 · 3rd 100G ♥1"), 82f, 52f, 18, null, null, 18f, 14f);
            EventCardKit.IconButton(crt, "Go", "Icon_Arrow", Loc.T("참가!", "Enter!"), new Color(1f, 0.52f, 0.10f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(440f, 84f), () => { Close(); onGo?.Invoke(); }, 32);
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.5f);
        }

        public static void ShowResult(Festival.Def d, int place, Action onDone)
        {
            Close();
            if (d == null) { onDone?.Invoke(); return; }
            CoastPrefs.VibrateEvent();   // 109차: 축제 결과 — 특정 이벤트 진동
            var crt = EventCardKit.Card("FestivalResultCanvas", 466, new Vector2(640f, 640f), out _canvas, 20f);   // 135차: 돌아가기 버튼이 축제 이름 줄을 가렸다 → 카드 80 키움
            Banner(crt, d, 24f, 130f);
            EventCardKit.Kid(crt, place <= 3 ? Loc.T("누나 대단해!", "You did it!") : Loc.T("다음엔 더 잘할 거야", "Next time!"), true);   // 109차: 꼬마 동행
            Color fill = place == 1 ? new Color(1f, 0.80f, 0.20f) : place == 2 ? new Color(0.80f, 0.82f, 0.90f) : place == 3 ? new Color(0.85f, 0.60f, 0.40f) : new Color(0.60f, 0.72f, 0.95f);
            EventCardKit.JellyTitle(crt, place <= 3 ? Loc.T($"{Festival.PlaceName(place)}!", $"{Festival.PlaceName(place)}!") : Loc.T("참가상", "Participation"), fill, new Color(0.35f, 0.20f, 0.05f), 176f, 100f, 60);
            EventCardKit.IconRow(crt, "Icon_Coin", new Color(1f, 0.85f, 0.45f), Loc.T($"+{Festival.Money(place)}G" + (Festival.Hearts(place) > 0 ? $" · ♥+{Festival.Hearts(place)}" : ""), $"+{Festival.Money(place)}G" + (Festival.Hearts(place) > 0 ? $" · ♥+{Festival.Hearts(place)}" : "")), 296f, 60f, 24);
            EventCardKit.IconRow(crt, "Icon_Star", new Color(0.75f, 0.62f, 1f), d.Name, 364f, 60f, 20);
            EventCardKit.IconButton(crt, "Ok", "Icon_Arrow", Loc.T("돌아가기", "Back"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(360f, 80f), () => { Close(); onDone?.Invoke(); }, 28);
            CoastAudioManager.PlayAnywhere(place <= 2 ? CoastSfx.RankS : CoastSfx.Coin, 0.7f);
        }

        /// 카드 위 축제 그림(UI_Fest_*, 없으면 계절색 띠).
        private static void Banner(RectTransform crt, Festival.Def d, float yTop, float h)
        {
            var tex = ArtAssets.LoadTexture(d.art);
            var im = CoastHudLayout.MakeImage(crt, "Art", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -yTop - h), new Vector2(-24f, -yTop),
                tex != null ? Color.white : (d.index == 0 ? new Color(0.98f, 0.85f, 0.30f) : d.index == 1 ? new Color(0.30f, 0.65f, 0.90f) : d.index == 2 ? new Color(0.98f, 0.60f, 0.20f) : new Color(0.55f, 0.60f, 0.85f)));
            im.raycastTarget = false;
            if (tex != null) { im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; EventCardKit.Animate(im); }   // 109차: 그림이 천천히 움직인다
            else
            {
                var t = CoastHudLayout.MakeText(im.rectTransform, "T", d.Name, 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.color = Color.white; t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.4f), 1.5f);
            }
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
