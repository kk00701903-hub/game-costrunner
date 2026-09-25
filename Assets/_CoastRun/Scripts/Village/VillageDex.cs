using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 195차(사용자: 「동물의 숲 급으로 NPC 도감 볼륨」 + 「도감 수집 판매(물고기·벌레·화석)」):
    /// 도감 5칸 — 물고기 · 벌레 · 광물·보석 · 화석 · 주민. 발견(dexSeen)·박물관 기증(dexDonated)·주민 호감도(npcFriend).
    public static class VillageDex
    {
        public struct Entry { public string key, ko, en, whereKo; public int cat; }   // cat 0 물고기 1 벌레 2 광물 3 화석
        public static readonly Entry[] Items = {
            new Entry { key = "fish_0", ko = "멸치", en = "Anchovy", whereKo = "바닷가 낚시 · 흔함", cat = 0 },
            new Entry { key = "fish_1", ko = "고등어", en = "Mackerel", whereKo = "바닷가 낚시", cat = 0 },
            new Entry { key = "fish_2", ko = "옥돔", en = "Tilefish", whereKo = "바닷가 낚시 · 귀함", cat = 0 },
            new Entry { key = "fish_3", ko = "갈치", en = "Hairtail", whereKo = "바닷가 낚시 · 귀함", cat = 0 },
            new Entry { key = "fish_4", ko = "다금바리", en = "Grouper", whereKo = "바닷가 낚시 · 전설", cat = 0 },
            new Entry { key = "fish_5", ko = "낡은 장화", en = "Old boot", whereKo = "바닷가 낚시 · 꽝", cat = 0 },
            new Entry { key = "bug_butterfly", ko = "나비", en = "Butterfly", whereKo = "언덕 풀밭 · 잠자리채", cat = 1 },
            new Entry { key = "bug_dragonfly", ko = "잠자리", en = "Dragonfly", whereKo = "바닷가 쪽 · 빠름", cat = 1 },
            new Entry { key = "bug_ladybug", ko = "무당벌레", en = "Ladybug", whereKo = "언덕 땅 위", cat = 1 },
            new Entry { key = "bug_bigbeetle", ko = "왕사슴벌레", en = "Giant stag beetle", whereKo = "언덕 · 방망이", cat = 1 },
            new Entry { key = "bug_hornet", ko = "왕말벌", en = "Giant hornet", whereKo = "언덕 · 방망이", cat = 1 },
            new Entry { key = "bug_crab", ko = "꽃게", en = "Crab", whereKo = "모래밭 · 방망이", cat = 1 },
            new Entry { key = "bug_hermit", ko = "소라게", en = "Hermit crab", whereKo = "모래밭", cat = 1 },
            new Entry { key = "bug_rare_butterfly", ko = "무지개나비", en = "Rainbow butterfly", whereKo = "어디서나 · 아주 드묾", cat = 1 },
            new Entry { key = "bug_rare_dragonfly", ko = "황금잠자리", en = "Golden dragonfly", whereKo = "바닷가 쪽 · 아주 드묾", cat = 1 },
            new Entry { key = "bug_rare_ladybug", ko = "칠보무당벌레", en = "Jewel ladybug", whereKo = "언덕 땅 위 · 아주 드묾", cat = 1 },
            new Entry { key = "bug_rare_beetle", ko = "황금사슴벌레", en = "Golden stag beetle", whereKo = "언덕 · 방망이 · 전설", cat = 1 },
            new Entry { key = "mat_stone", ko = "돌", en = "Stone", whereKo = "언덕 바위 · 곡괭이", cat = 2 },
            new Entry { key = "ore_iron", ko = "철광석", en = "Iron ore", whereKo = "오름 광산 · 붉은 광맥", cat = 2 },
            new Entry { key = "ore_silver", ko = "은광석", en = "Silver ore", whereKo = "오름 광산 · 흰 광맥", cat = 2 },
            new Entry { key = "ore_gold", ko = "금광석", en = "Gold ore", whereKo = "오름 광산 · 금빛 광맥", cat = 2 },
            new Entry { key = "gem_amethyst", ko = "자수정", en = "Amethyst", whereKo = "오름 광산 · 보라 광맥", cat = 2 },
            new Entry { key = "gem_jade", ko = "비취", en = "Jade", whereKo = "오름 광산 · 보라 광맥(드묾)", cat = 2 },
            new Entry { key = "gem_ruby", ko = "루비", en = "Ruby", whereKo = "오름 광산 · 보라 광맥(아주 드묾)", cat = 2 },
            new Entry { key = "fossil_ammonite", ko = "암모나이트 화석", en = "Ammonite", whereKo = "오름 광산 · 크림색 광맥", cat = 3 },
            new Entry { key = "fossil_trilobite", ko = "삼엽충 화석", en = "Trilobite", whereKo = "오름 광산 · 크림색 광맥", cat = 3 },
            new Entry { key = "fossil_fern", ko = "고사리 화석", en = "Fern fossil", whereKo = "오름 광산 · 크림색 광맥", cat = 3 },
            new Entry { key = "fossil_shark", ko = "상어 이빨 화석", en = "Shark tooth", whereKo = "오름 광산 · 크림색 광맥(드묾)", cat = 3 },
        };

        public struct Npc { public string key, ko, en, placeKo, likes, likesKo, lineKo; public int season; }
        /// 주민 20명. 0~5 는 마을을 걷는 사람(VillageCreatures.Defs 순서), 나머지는 가게·시설 사람.
        public static readonly Npc[] Npcs = {
            new Npc { key = "v0", ko = "해녀 할머니", en = "Haenyeo grandma", placeKo = "바닷가", likes = "gath_shell", likesKo = "조개껍데기", lineKo = "물질 50년. 바다가 오늘 기분이 좋구먼.", season = 1 },
            new Npc { key = "v1", ko = "등대지기", en = "Lighthouse keeper", placeKo = "등대 옆 집", likes = "mat_wood", likesKo = "장작", lineKo = "밤마다 불을 켜. 누군가는 이 불을 보고 집에 오거든.", season = 3 },
            new Npc { key = "v2", ko = "꽃집 언니", en = "Florist", placeKo = "마을 골목", likes = "flower_rose", likesKo = "장미", lineKo = "유채꽃 필 때가 제일 바빠. 그래도 제일 좋아!", season = 0 },
            new Npc { key = "v3", ko = "낚시 소년", en = "Fisher boy", placeKo = "바닷가", likes = "ing_fish", likesKo = "생선", lineKo = "다금바리 한 번만 잡아 봤으면…!", season = 1 },
            new Npc { key = "v4", ko = "카페 알바", en = "Cafe clerk", placeKo = "반도 바다 카페", likes = "honey_jar", likesKo = "꿀", lineKo = "라떼 아트 연습 중이에요. 하트가 자꾸 감자가 돼요.", season = 2 },
            new Npc { key = "v5", ko = "서퍼", en = "Surfer", placeKo = "바닷가", likes = "gath_coconut", likesKo = "코코넛", lineKo = "파도는 기다리는 사람한테 와.", season = 1 },
            new Npc { key = "shop", ko = "상점 아줌마", en = "Shopkeeper", placeKo = "마을상점", likes = "crop_tomato", likesKo = "토마토", lineKo = "싸고 좋은 거만 들여놔. 흥정은 안 돼~", season = 2 },
            new Npc { key = "job", ko = "알바나라 아저씨", en = "Job clerk", placeKo = "알바나라", likes = "ing_egg", likesKo = "달걀", lineKo = "성실하면 일은 늘 있어.", season = 3 },
            new Npc { key = "nurse", ko = "간호사", en = "Nurse", placeKo = "병원", likes = "med_vitamin", likesKo = "종합비타민", lineKo = "바다엔 10초 넘게 있으면 안 돼요!", season = 0 },
            new Npc { key = "brunch", ko = "귤빛 브런치 사장님", en = "Brunch owner", placeKo = "동쪽 새 동네", likes = "fruit_tangerine", likesKo = "감귤", lineKo = "우리 팬케이크 귤은 전부 이 동네 거야.", season = 2 },
            new Npc { key = "teller", ko = "은행원", en = "Bank teller", placeKo = "제주 시내 은행", likes = "gem_amethyst", likesKo = "자수정", lineKo = "이자는 성실한 사람 편이에요.", season = 3 },
            new Npc { key = "boutique", ko = "부티크 점원", en = "Boutique clerk", placeKo = "탐라 부티크", likes = "flower_lavender", likesKo = "라벤더", lineKo = "오늘의 추천은 귤빛 후드!", season = 0 },
            new Npc { key = "dj", ko = "청음샵 DJ", en = "Record DJ", placeKo = "섬소리 청음샵", likes = "souv_choco", likesKo = "귤 초콜릿", lineKo = "이 앨범 3번 트랙, 이어폰으로 들어 봐.", season = 3 },
            new Npc { key = "noodle", ko = "국수집 사장님", en = "Noodle chef", placeKo = "올레 고기국수", likes = "ing_meat", likesKo = "고기", lineKo = "국물은 밤새 끓인 거야.", season = 3 },
            new Npc { key = "pork", ko = "흑돼지집 사장님", en = "Pork chef", placeKo = "돔베 흑돼지", likes = "ing_spice", likesKo = "양념", lineKo = "근고기는 두껍게! 그게 제주식이지.", season = 2 },
            new Npc { key = "mart", ko = "마트 점원", en = "Mart clerk", placeKo = "시내 마트", likes = "gift_snack", likesKo = "선물 과자", lineKo = "계산은 이쪽이에요~ 봉투 필요하세요?", season = 1 },
            new Npc { key = "curator", ko = "박물관 큐레이터", en = "Curator", placeKo = "민속자연사박물관", likes = "fossil_ammonite", likesKo = "암모나이트 화석", lineKo = "빈 진열장이 채워질 때가 제일 설레요.", season = 0 },
            new Npc { key = "market", ko = "축협 아저씨", en = "Market man", placeKo = "축협 가축시장", likes = "ing_milk", likesKo = "우유", lineKo = "소는 느긋하게 키워야 살이 올라.", season = 2 },
            new Npc { key = "souv", ko = "기념품 가게 언니", en = "Souvenir clerk", placeKo = "중문관광단지", likes = "fruit_hallabong", likesKo = "한라봉", lineKo = "하르방 인형 코를 만지면 아들 낳는대요. 농담~", season = 1 },
            new Npc { key = "miner", ko = "광부 할아버지", en = "Old miner", placeKo = "오름 광산 입구", likes = "ore_gold", likesKo = "금광석", lineKo = "보라빛 결이 보이면 천천히 두드려.", season = 3 },
        };
        public static int NpcIndex(string key) { for (int i = 0; i < Npcs.Length; i++) if (Npcs[i].key == key) return i; return -1; }

        public static void Ensure(SaveData s)
        {
            if (s == null) return;
            if (s.dexSeen == null) s.dexSeen = new List<string>(); if (s.dexDonated == null) s.dexDonated = new List<string>();
            if (s.npcFriend == null || s.npcFriend.Length < 32) { var a = new int[32]; if (s.npcFriend != null) Array.Copy(s.npcFriend, a, s.npcFriend.Length); s.npcFriend = a; }
            if (s.npcTalkStamp == null || s.npcTalkStamp.Length < 32) { var a = new int[32]; for (int i = 0; i < a.Length; i++) a[i] = -1; if (s.npcTalkStamp != null) Array.Copy(s.npcTalkStamp, a, s.npcTalkStamp.Length); s.npcTalkStamp = a; }
            if (s.outfits == null) s.outfits = new List<string>();
            if (s.mineDone == null) s.mineDone = new List<int>();
            if (s.tourStamp == null || s.tourStamp.Length < 5) s.tourStamp = new[] { -1, -1, -1, -1, -1 };
            if (s.calfAge == null) s.calfAge = new int[0]; if (s.pigAge == null) s.pigAge = new int[0];
        }

        /// 새로 발견했으면 true(연출용)
        public static bool See(SaveData s, string key)
        {
            if (s == null || string.IsNullOrEmpty(key)) return false; Ensure(s);
            if (s.dexSeen.Contains(key)) return false;
            bool tracked = false; foreach (var e in Items) if (e.key == key) { tracked = true; break; }
            if (!tracked && !key.StartsWith("npc_")) return false;
            s.dexSeen.Add(key);
            if (tracked) { string nm = key; foreach (var e in Items) if (e.key == key) nm = Loc.T(e.ko, e.en); CoastToast.Pop(Loc.T($"📖 도감 새 발견! {nm}", $"📖 New entry! {nm}")); }
            return true;
        }
        public static bool Donatable(string key) => key.StartsWith("bug_") || key.StartsWith("ore_") || key.StartsWith("gem_") || key.StartsWith("fossil_");
        public static int Count(SaveData s, int cat) { Ensure(s); int n = 0; foreach (var e in Items) if (e.cat == cat && s.dexSeen.Contains(e.key)) n++; return n; }
        public static int Total(int cat) { int n = 0; foreach (var e in Items) if (e.cat == cat) n++; return n; }
        public static int Friend(SaveData s, int i) { Ensure(s); return i >= 0 && i < s.npcFriend.Length ? s.npcFriend[i] : 0; }
        public static string Hearts(int f) { int h = Mathf.Clamp(f / 20, 0, 5); return new string('♥', h) + new string('♡', 5 - h); }

        /// 호감도 올리기 — 20·50·80·100 을 넘으면 보상 + 큰 글씨 연출. 돌려주는 값 = 넘은 단계(0 없음).
        public static int AddFriend(SaveData s, int i, int amt)
        {
            Ensure(s); if (i < 0 || i >= Npcs.Length) return 0;
            int before = s.npcFriend[i]; int after = Mathf.Clamp(before + amt, 0, 100); s.npcFriend[i] = after;
            See(s, "npc_" + Npcs[i].key);
            int[] steps = { 20, 50, 80, 100 }; int crossed = 0;
            for (int k = 0; k < steps.Length; k++) if (before < steps[k] && after >= steps[k]) crossed = k + 1;
            if (crossed > 0)
            {
                int money = crossed == 1 ? 60 : crossed == 2 ? 150 : crossed == 3 ? 300 : 600;
                s.stats.money += money; s.starShards += crossed; s.starShardsTotal += crossed;
                string lv = crossed == 1 ? Loc.T("아는 사이", "Acquaintance") : crossed == 2 ? Loc.T("친구", "Friend") : crossed == 3 ? Loc.T("단짝", "Close friend") : Loc.T("평생 친구", "Best friend");
                CoastToast.Pop(Loc.T($"💗 {Npcs[i].ko}와(과) {lv}! 선물 +{money}G · 별조각 +{crossed}", $"💗 {Npcs[i].en}: {lv}! +{money}G"));
                CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.7f);
            }
            return crossed;
        }
        /// 대화 한 번(한 페이즈에 한 번만 호감 +amt)
        public static void Talk(SaveData s, int i, int amt = 2)
        {
            Ensure(s); if (i < 0 || i >= Npcs.Length || s == null) return;
            int stamp = s.week * 4 + s.phaseIndex; if (s.npcTalkStamp[i] == stamp) { See(s, "npc_" + Npcs[i].key); return; }
            s.npcTalkStamp[i] = stamp; AddFriend(s, i, amt);
        }
        /// 호감도에 따라 바뀌는 한마디
        public static string Line(SaveData s, int i)
        {
            if (i < 0 || i >= Npcs.Length) return ""; int f = Friend(s, i); var n = Npcs[i];
            if (f < 20) return n.lineKo;
            if (f < 50) return Loc.T($"또 왔네! 난 {n.likesKo} 좋아해. 기억해 줘~", $"You again! I love {n.likesKo}.");
            if (f < 80) return Loc.T("너 오면 하루가 밝아져. 오늘은 뭐 했어?", "My day brightens when you come by.");
            return Loc.T("우린 평생 친구야. 무슨 일 있으면 꼭 말해!", "Friends for life. Tell me anything!");
        }

        // ── 도감 창 ────────────────────────────────────────────────────
        static GameObject _ui; static int _tab;
        public static bool Open => _ui != null;
        public static void Show(SaveData s, Action<int> onGift, Action onClose, int tab = 0)
        {
            Close(); Ensure(s); _tab = tab;
            var cv = CoastUiCanvas.Create("VillageDex", 175); _ui = cv.gameObject;
            var root = CoastUiCanvas.Root(cv);
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.45f)); dim.raycastTarget = true;
            var card = CoastUiArt.Panel(root, "Card", new Color(1f, 0.98f, 0.93f), 28); var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(680f, 1080f); card.raycastTarget = true;
            var title = CoastHudLayout.MakeText(crt, "T", Loc.T("📖 제주 생활 도감", "📖 Jeju Life Encyclopedia"), 32, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -74f), new Vector2(-20f, -16f));
            title.color = new Color(0.36f, 0.22f, 0.14f); title.fontStyle = FontStyle.Bold;
            string[] tabs = { Loc.T("물고기", "Fish"), Loc.T("벌레", "Bugs"), Loc.T("광물·보석", "Minerals"), Loc.T("화석", "Fossils"), Loc.T("주민", "People") };
            Color[] tc = { new Color(0.40f, 0.70f, 0.95f), new Color(0.55f, 0.80f, 0.45f), new Color(0.72f, 0.55f, 0.92f), new Color(0.85f, 0.70f, 0.45f), new Color(0.98f, 0.55f, 0.70f) };
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i; var tb = CoastUiArt.GlossyPill(crt, "Tab" + i, i == tab ? tc[i] : Color.Lerp(tc[i], Color.white, 0.55f), 16, 5); tb.raycastTarget = true;
                var trt = tb.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f); trt.pivot = new Vector2(0f, 1f); trt.anchoredPosition = new Vector2(18f + i * 130f, -84f); trt.sizeDelta = new Vector2(122f, 54f);
                string cnt = i < 4 ? $"{Count(s, i)}/{Total(i)}" : $"{CountNpcSeen(s)}/{Npcs.Length}";
                var tt = CoastHudLayout.MakeText(trt, "T", tabs[i] + "\n" + cnt, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f)); tt.color = Color.white; tt.fontStyle = FontStyle.Bold;
                tb.gameObject.AddComponent<Button>().onClick.AddListener(() => Show(s, onGift, onClose, k));
            }
            // 스크롤 목록
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); vp.transform.SetParent(crt, false);
            var vrt = vp.GetComponent<RectTransform>(); vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f); vrt.offsetMin = new Vector2(18f, 96f); vrt.offsetMax = new Vector2(-18f, -150f);
            vp.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f); vp.GetComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(vrt, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            var sr = vp.AddComponent<ScrollRect>(); sr.content = content; sr.horizontal = false; sr.viewport = vrt; sr.movementType = ScrollRect.MovementType.Clamped;
            float y = 0f; const float RowH = 96f;
            void Row(string head, string sub, Color fill, string btn, Action onBtn)
            {
                var r = CoastUiArt.Panel(content, "Row", fill, 16); r.raycastTarget = false; var rt = r.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -y); rt.sizeDelta = new Vector2(0f, RowH - 8f);
                var h = CoastHudLayout.MakeText(rt, "H", head, 22, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(onBtn != null ? -150f : -12f, -4f)); h.color = new Color(0.30f, 0.20f, 0.14f); h.fontStyle = FontStyle.Bold;
                var su = CoastHudLayout.MakeText(rt, "S", sub, 15, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(18f, 4f), new Vector2(onBtn != null ? -150f : -12f, 0f)); su.color = new Color(0.40f, 0.34f, 0.44f); su.horizontalOverflow = HorizontalWrapMode.Wrap;
                su.resizeTextForBestFit = true; su.resizeTextMinSize = 10; su.resizeTextMaxSize = 15;
                if (onBtn != null)
                {
                    var b = CoastUiArt.GlossyPill(rt, "Btn", new Color(0.98f, 0.55f, 0.70f), 14, 5); b.raycastTarget = true; var brt = b.rectTransform;
                    brt.anchorMin = brt.anchorMax = new Vector2(1f, 0.5f); brt.pivot = new Vector2(1f, 0.5f); brt.anchoredPosition = new Vector2(-10f, 0f); brt.sizeDelta = new Vector2(128f, 50f);
                    var bt = CoastHudLayout.MakeText(brt, "T", btn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(2f, 2f), Vector2.zero); bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
                    b.gameObject.AddComponent<Button>().onClick.AddListener(() => onBtn());
                }
                y += RowH;
            }
            if (tab < 4)
            {
                foreach (var e in Items)
                {
                    if (e.cat != tab) continue;
                    bool seen = s.dexSeen.Contains(e.key); bool don = s.dexDonated.Contains(e.key);
                    Row(seen ? Loc.T(e.ko, e.en) + (don ? "  🏛" : "") : "？？？", seen ? Loc.T($"사는 곳: {e.whereKo}" + (don ? " · 박물관에 전시 중" : Donatable(e.key) ? " · 시내 박물관에 기증할 수 있다" : ""), e.whereKo) : Loc.T($"힌트: {e.whereKo}", e.whereKo),
                        seen ? new Color(1f, 0.96f, 0.86f) : new Color(0.90f, 0.90f, 0.92f), null, null);
                }
            }
            else
            {
                for (int i = 0; i < Npcs.Length; i++)
                {
                    var n = Npcs[i]; bool seen = s.dexSeen.Contains("npc_" + n.key); int f = Friend(s, i); int k = i;
                    string season = Timeline.SeasonName((SeasonKind)n.season);
                    Row(seen ? $"{n.ko}  {Hearts(f)}" : "？？？", seen ? Loc.T($"{n.placeKo} · 좋아하는 것: {n.likesKo} · 생일 {season}\n「{Line(s, i)}」", n.en) : Loc.T($"만날 수 있는 곳: {n.placeKo}", n.placeKo),
                        seen ? new Color(1f, 0.93f, 0.95f) : new Color(0.90f, 0.90f, 0.92f), seen && onGift != null ? Loc.T("🎁 선물", "🎁 Gift") : null, seen && onGift != null ? (Action)(() => onGift(k)) : null);
                }
            }
            content.sizeDelta = new Vector2(0f, y + 10f);
            var close = CoastUiArt.GlossyPill(crt, "Close", new Color(0.55f, 0.57f, 0.64f), 18, 6); close.raycastTarget = true; var cl = close.rectTransform;
            cl.anchorMin = cl.anchorMax = new Vector2(0.5f, 0f); cl.pivot = new Vector2(0.5f, 0f); cl.anchoredPosition = new Vector2(0f, 20f); cl.sizeDelta = new Vector2(280f, 60f);
            var ct = CoastHudLayout.MakeText(cl, "T", Loc.T("닫기", "Close"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            close.gameObject.AddComponent<Button>().onClick.AddListener(() => { Close(); onClose?.Invoke(); });
        }
        public static void Close() { if (_ui != null) UnityEngine.Object.Destroy(_ui); _ui = null; }
        static int CountNpcSeen(SaveData s) { int n = 0; foreach (var e in Npcs) if (s.dexSeen.Contains("npc_" + e.key)) n++; return n; }
    }
}
