using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 105차(재미요소 P2-2, 프린세스 메이커): 엔딩 시네마 뒤 **「스무 살 하늘」 에필로그 카드 한 장**.
    ///   가장 많이 한 알바·가장 높은 스탯으로 고른 한 컷(그림 Resources/CoastRun/UI_Epi_<key>, 없으면 색 띠) + 두 줄 자막(겪은 일만, 해설 없음) + 회차 요약.
    ///   엔딩 A/B/진엔딩 시네마와 분기 규칙은 그대로. 이 카드는 그 뒤에 한 번.
    public static class EpilogueUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public class Line { public string key, ko, en; public string[] jobs; public StatKind stat; }
        public static readonly Line[] Lines =
        {
            new Line { key = "Cafe",     jobs = new[] { "job_cafe", "job_sashimi", "job_hall", "job_salon" }, stat = StatKind.Charm,
                ko = "스무 살 봄. 아침 여섯 시에 가게 문을 연다.\n첫 손님은 늘 우유 두 병을 산다.", en = "Twenty, spring. She opens the shop at six.\nThe first customer always buys two bottles of milk." },
            new Line { key = "Sea",      jobs = new[] { "job_haenyeo", "les_swim", "rest_sea" }, stat = StatKind.Stamina,
                ko = "스무 살 여름. 해녀복 지퍼를 혼자 올린다.\n부표에 그려 둔 하트가 아직 안 지워졌다.", en = "Twenty, summer. She zips the wetsuit herself.\nThe heart on the buoy hasn't washed off." },
            new Line { key = "Radio",    jobs = new[] { "dev_radio", "les_ham", "job_dj_assist" }, stat = StatKind.Sense,
                ko = "스무 살 밤. 89.2에 사연이 하나 들어왔다.\n읽기 전에 물을 한 모금 마신다.", en = "Twenty, night. A letter came in on 89.2.\nShe drinks some water before reading it." },
            new Line { key = "Delivery", jobs = new[] { "job_delivery", "job_night_delivery", "job_market", "job_orange" }, stat = StatKind.Agility,
                ko = "스무 살 가을. 귤 상자를 스쿠터에 세 개 싣는다.\n언덕 위 집까지 8분.", en = "Twenty, autumn. Three crates of tangerines on the scooter.\nEight minutes to the house on the hill." },
            new Line { key = "Tower",    jobs = new[] { "job_tower_fix", "job_tower_watch", "job_lighthouse" }, stat = StatKind.Stamina,
                ko = "스무 살 겨울. 송전탑 아래 돌 세 개를 다시 쌓는다.\n장갑을 벗어야 돌이 잘 잡힌다.", en = "Twenty, winter. She restacks the three stones under the tower.\nThe stones grip better without gloves." },
            new Line { key = "Stage",    jobs = new[] { "dev_dance", "les_dance" }, stat = StatKind.Charm,
                ko = "스무 살. 마을회관 무대에 혼자 선다.\n스피커에서 아빠의 곡이 나온다.", en = "Twenty. She stands alone on the village hall stage.\nDad's song plays from the speaker." },
            new Line { key = "Runner",   jobs = new[] { "dev_skate", "les_skate", "dev_oreum", "les_gym" }, stat = StatKind.Agility,
                ko = "스무 살. 해안도로를 끝까지 달렸다.\n송전탑 아래서 숨을 고른다.", en = "Twenty. She ran the coast road to the end.\nShe catches her breath under the tower." },
        };

        /// 회차 기록으로 고르기 — 가장 많이 한 카드 묶음, 동률이면 가장 높은 스탯.
        public static Line Pick(SaveData s)
        {
            if (s == null) return Lines[Lines.Length - 1];
            Line best = null; int bestN = -1;
            foreach (var l in Lines)
            {
                int n = 0; foreach (var id in l.jobs) n += RaisingFun.MasteryUses(s, id);
                if (n > bestN) { bestN = n; best = l; }
            }
            if (bestN <= 0)
            {
                var st = s.stats; StatKind top = StatKind.Stamina; int tv = st.stamina;
                if (st.agility > tv) { top = StatKind.Agility; tv = st.agility; }
                if (st.charm > tv) { top = StatKind.Charm; tv = st.charm; }
                if (st.sense > tv) { top = StatKind.Sense; tv = st.sense; }
                foreach (var l in Lines) if (l.stat == top) return l;
            }
            return best ?? Lines[Lines.Length - 1];
        }

        public static void Show(SaveData s, Action onDone)
        {
            Close();
            if (s == null) { onDone?.Invoke(); return; }
            var line = Pick(s);
            var crt = EventCardKit.Card("EpilogueCanvas", 480, new Vector2(640f, 960f), out _canvas, 0f);   // 135차: 「다음」 버튼이 요약 줄을 가렸다 → 60 키움
            var tex = ArtAssets.LoadTexture("UI_Epi_" + line.key);
            var im = CoastHudLayout.MakeImage(crt, "Art", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f - 380f), new Vector2(-24f, -24f), tex != null ? Color.white : new Color(0.55f, 0.62f, 0.85f));
            im.raycastTarget = false;
            if (tex != null) { im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; EventCardKit.Animate(im); }   // 109차: 그림이 천천히 움직인다
            EventCardKit.JellyTitle(crt, Loc.T("스무 살, 하늘", "Haneul, Twenty"), new Color(0.45f, 0.35f, 0.95f), new Color(0.20f, 0.12f, 0.45f), 420f, 80f, 40);
            var box = EventCardKit.InfoBox(crt, 510f, 150f);
            var body = CoastHudLayout.MakeText(box, "Body", Loc.T(line.ko, line.en), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            body.color = EventCardKit.Ink; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            // 회차 요약 — 스탯·숙련 ★·단서·축제
            int stars = 0; string topCard = null; int topN = 0;
            if (s.masteryIds != null) for (int i = 0; i < s.masteryIds.Length; i++) { int lv = RaisingFun.MasteryLevel(s.masteryCounts[i]); stars += lv - 1; if (s.masteryCounts[i] > topN) { topN = s.masteryCounts[i]; var d = ScheduleTable.Get(s.masteryIds[i]); topCard = d != null ? d.Name : null; } }
            int fest = 0; if (s.festivalPlace != null) foreach (var p in s.festivalPlace) if (p >= 1 && p <= 3) fest++;
            EventCardKit.IconRow(crt, "Icon_Star", new Color(0.75f, 0.62f, 1f), Loc.T($"체력 {s.stats.stamina} · 순발력 {s.stats.agility} · 매력 {s.stats.charm} · 감성 {s.stats.sense}", $"STA {s.stats.stamina} · AGI {s.stats.agility} · CHA {s.stats.charm} · SEN {s.stats.sense}"), 676f, 52f, 16);
            EventCardKit.IconRow(crt, "Icon_Coin", new Color(1f, 0.85f, 0.45f), Loc.T($"숙련 ★{stars}" + (topCard != null ? $" (제일 많이: {topCard})" : "") + $" · 단서 {ClueSystem.Count(s)}/6 · 축제 입상 {fest}", $"Mastery ★{stars}" + (topCard != null ? $" (most: {topCard})" : "") + $" · clues {ClueSystem.Count(s)}/6 · festival podiums {fest}"), 736f, 52f, 16);
            EventCardKit.IconButton(crt, "Ok", "Icon_Arrow", Loc.T("다음", "Next"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(320f, 74f), () => { Close(); onDone?.Invoke(); }, 28);
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.5f);
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
