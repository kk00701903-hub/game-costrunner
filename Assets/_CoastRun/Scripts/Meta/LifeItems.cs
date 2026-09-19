using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 다마고치 + 프린세스 메이커 느낌의 생활 아이템.
    ///   재료(싸게) → 방 조리 → 기본요리 → 「밥」으로 식사 시 소진.
    ///   고급요리·약은 바로 사용 가능. 옷은 기존 clothesWeeks 와 연동.
    public enum LifeItemCat
    {
        Ingredient = 0,
        BasicDish = 1,
        PremiumDish = 2,
        Medicine = 3,
        Care = 4,
        Clothes = 5,
        Gather = 6,   // 150차: 채집(작물·장작·벌레·조개·버섯)
    }

    [Serializable]
    public class LifeStack
    {
        public string id;
        public int n;
    }

    public struct LifeItemDef
    {
        public string id;
        public string nameKo, nameEn, blurbKo, blurbEn;
        public LifeItemCat cat;
        public int price;          // 상점 단가(G). 0 = 비매품(조리·수확 전용)
        public bool shop;          // 일반상점에 노출
        public string art;         // Resources 키(없으면 카테고리 기본)
        public string cookTo;      // 재료 → 기본요리 id
        public int hunger, condition, stress, stamina; // 사용/식사 효과
        public bool edible;        // 밥 버튼으로 먹을 수 있음
        public bool usable;        // 인벤에서 바로 사용(약·케어)
        public bool clothesPack;   // 사용 시 clothesWeeks 리필
    }

    public static class LifeItems
    {
        public static readonly LifeItemDef[] All =
        {
            // ── 재료(싸게, 조리 필요) ──
            D("ing_rice", "쌀", "Rice", "싸게 사 두고 방에서 밥으로 지어 먹어요.", "Cheap staple — cook into rice at home.",
                LifeItemCat.Ingredient, 18, true, "UI_Goods_Rice", "dish_rice", 0, 0, 0, 0, false, false, false),
            D("ing_veg", "채소", "Vegetables", "텃밭·상점에서. 볶음·국으로 조리.", "From shop or garden. Cook into a side.",
                LifeItemCat.Ingredient, 12, true, "UI_Goods_Side", "dish_veg", 0, 0, 0, 0, false, false, false),
            D("ing_meat", "고기", "Meat", "단백질. 불고기·찌개로 조리.", "Protein — cook into a meat dish.",
                LifeItemCat.Ingredient, 28, true, "UI_Goods_Side", "dish_meat", 0, 0, 0, 0, false, false, false),
            D("ing_egg", "달걀", "Eggs", "간단 조리용. 계란밥·프라이.", "Quick cook — egg rice / fry.",
                LifeItemCat.Ingredient, 10, true, "UI_Goods_Side", "dish_egg", 0, 0, 0, 0, false, false, false),
            D("ing_milk", "우유", "Milk", "음료·디저트 재료. 따뜻한 우유로.", "Drink or dessert — warm milk.",
                LifeItemCat.Ingredient, 14, true, "UI_Goods_Side", "dish_milk", 0, 0, 0, 0, false, false, false),
            D("ing_fish", "생선", "Fish", "제주 생선. 구이로 조리.", "Jeju fish — grill it at home.",
                LifeItemCat.Ingredient, 32, true, "UI_Goods_Side", "dish_fish", 0, 0, 0, 0, false, false, false),
            D("ing_spice", "양념", "Seasoning", "요리를 한 단계 업. 고급 요리 재료.", "Upgrade cook — premium recipe part.",
                LifeItemCat.Ingredient, 16, true, "UI_Goods_Side", "dish_premium_stew", 0, 0, 0, 0, false, false, false),

            // ── 기본요리(조리 결과 / 비싸게 완제품도 판매) ──
            D("dish_rice", "흰밥", "Cooked Rice", "기본 주식. 배부름 ↑", "Staple meal. Fullness ↑",
                LifeItemCat.BasicDish, 55, true, "UI_Goods_Rice", null, 32, 2, -2, 1, true, false, false),
            D("dish_veg", "야채볶음", "Veg Stir-fry", "반찬. 배부름·컨디션 살짝 ↑", "Side dish. Fullness & condition ↑",
                LifeItemCat.BasicDish, 40, true, "UI_Goods_Side", null, 14, 6, -1, 0, true, false, false),
            D("dish_meat", "불고기", "Bulgogi", "고기 요리. 배부름·체력 ↑", "Meat dish. Fullness & stamina ↑",
                LifeItemCat.BasicDish, 70, true, "UI_Goods_Side", null, 28, 5, -3, 2, true, false, false),
            D("dish_egg", "계란밥", "Egg Rice", "간단 한 끼. 배부름 ↑", "Quick meal. Fullness ↑",
                LifeItemCat.BasicDish, 35, true, "UI_Goods_Rice", null, 22, 3, -2, 1, true, false, false),
            D("dish_milk", "따뜻한 우유", "Warm Milk", "잠 오기 전에. 스트레스 ↓", "Before bed. Stress ↓",
                LifeItemCat.BasicDish, 30, true, "UI_Goods_Side", null, 8, 2, -8, 0, true, false, false),
            D("dish_fish", "생선구이", "Grilled Fish", "담백한 한 끼. 컨디션 ↑", "Light meal. Condition ↑",
                LifeItemCat.BasicDish, 75, true, "UI_Goods_Side", null, 26, 8, -2, 1, true, false, false),

            // ── 고급요리 ──
            D("dish_premium_stew", "제주 해물찌개", "Jeju Seafood Stew", "진한 한 끼. 배부름·컨디션 크게 ↑", "Hearty stew. Big fullness & condition.",
                LifeItemCat.PremiumDish, 140, true, "UI_Goods_Side", null, 45, 14, -6, 3, true, false, false),
            D("dish_cake", "수제 케이크", "Homemade Cake", "달콤한 위로. 스트레스 크게 ↓", "Sweet comfort. Stress ↓↓",
                LifeItemCat.PremiumDish, 120, true, "UI_Goods_Side", null, 12, 4, -18, 0, true, false, false),
            D("dish_bento", "특제 도시락", "Special Bento", "알차게. 체력·매력 기분 ↑", "Filling. Stamina boost.",
                LifeItemCat.PremiumDish, 110, true, "UI_Goods_Rice", null, 38, 8, -4, 4, true, false, false),
            D("dish_soup", "삼계탕", "Ginseng Chicken Soup", "보양식. 컨디션·체력 ↑↑", "Tonic soup. Condition & stamina ↑↑",
                LifeItemCat.PremiumDish, 160, true, "UI_Goods_Side", null, 40, 18, -5, 5, true, false, false),

            // ── 약 ──
            D("med_cold", "감기약", "Cold Medicine", "컨디션 회복. 몸살에.", "Restores condition.",
                LifeItemCat.Medicine, 80, true, "Obs_Potion", null, 0, 25, -4, 0, false, true, false),
            D("med_vitamin", "종합비타민", "Vitamins", "매일 한 알. 컨디션·체력 소폭 ↑", "Daily tonic. Mild condition/stamina.",
                LifeItemCat.Medicine, 50, true, "Obs_Potion", null, 0, 10, 0, 2, false, true, false),
            D("med_stomach", "소화제", "Digestive", "배 아플 때. 배부름 안정.", "Settles stomach.",
                LifeItemCat.Medicine, 40, true, "Obs_Potion", null, 8, 8, -2, 0, false, true, false),
            D("med_energy", "에너지 드링크", "Energy Drink", "당장 체력 ↑ · 스트레스 살짝 ↑", "Stamina now · slight stress.",
                LifeItemCat.Medicine, 60, true, "Obs_Potion", null, 0, -2, 4, 8, false, true, false),

            // ── 케어(프메 느낌) ──
            D("care_soap", "좋은 비누", "Nice Soap", "씻고 나면 기분 좋아. 스트레스 ↓", "Feel fresh. Stress ↓",
                LifeItemCat.Care, 35, true, "UI_Goods_Shirt", null, 0, 3, -10, 0, false, true, false),
            D("care_lotion", "핸드크림", "Hand Cream", "손 보습. 매력 기분 ↑", "Soft hands. Tiny charm mood.",
                LifeItemCat.Care, 45, true, "UI_Goods_Shirt", null, 0, 2, -6, 0, false, true, false),
            D("care_perfume", "향수", "Perfume", "외출 전. 매력·평판 기분 ↑", "Before going out. Charm mood.",
                LifeItemCat.Care, 90, true, "UI_Goods_Shirt", null, 0, 2, -4, 0, false, true, false),
            D("care_book", "에세이 책", "Essay Book", "읽고 쉬기. 감성·스트레스 ↓", "Quiet reading. Sense / stress ↓",
                LifeItemCat.Care, 55, true, "Icon_Book", null, 0, 0, -12, 0, false, true, false),

            // ── 150차: 채집 ──
            D("crop_tomato", "토마토", "Tomato", "텃밭에서 키운 것. 그냥 먹어도 맛있다.", "Home-grown. Tasty as is.",
                LifeItemCat.Gather, 0, false, "UI_Goods_Side", "dish_veg", 12, 4, -1, 0, true, false, false),
            D("crop_potato", "감자", "Potato", "텃밭 감자. 든든하다.", "Home-grown potato. Filling.",
                LifeItemCat.Gather, 0, false, "UI_Goods_Side", "dish_veg", 16, 3, 0, 1, true, false, false),
            D("crop_rice", "볏단", "Rice Sheaf", "텃밭 벼. 방에서 쌀로.", "Home-grown rice. Cook into rice.",
                LifeItemCat.Gather, 0, false, "UI_Goods_Rice", "dish_rice", 0, 0, 0, 0, false, false, false),
            D("flower_rose", "장미 한 송이", "Rose", "책상에 꽂아 두면 마음이 편해진다. 스트레스 -25", "On the desk it calms you. Stress -25",
                LifeItemCat.Gather, 0, false, "UI_Goods_Shirt", null, 0, 2, -25, 0, false, true, false),
            D("flower_lavender", "라벤더 다발", "Lavender", "향이 좋다. 스트레스 -12", "Smells lovely. Stress -12",
                LifeItemCat.Gather, 0, false, "UI_Goods_Shirt", null, 0, 2, -12, 0, false, true, false),
            D("mat_wood", "장작", "Firewood", "나무를 도끼로 패거나 나뭇가지를 주워 모은다. 우리집 난로 연료(1주에 1개).", "Chop trees or pick up branches. Fuel for the home stove (1 per week).",
                LifeItemCat.Gather, 0, false, "Icon_Book", null, 0, 0, 0, 0, false, false, false),
            D("mat_stone", "돌", "Stone", "언덕 바위를 곡괭이로 캔 돌. 집·마당 꾸미기 재료.", "Mined from hill rocks. Building material.",
                LifeItemCat.Gather, 0, false, "Icon_Book", null, 0, 0, 0, 0, false, false, false),
            D("gath_coconut", "코코넛", "Coconut", "야자수를 흔들면 떨어진다. 시원하다.", "Falls from palms. Refreshing.",
                LifeItemCat.Gather, 0, false, "UI_Goods_Side", null, 10, 3, -4, 1, true, false, false),
            D("gath_shell", "조개껍데기", "Seashell", "바닷가에서 주운 예쁜 조개. 보고 있으면 기분이 좋다. 스트레스 -6", "A pretty shell from the beach. Stress -6",
                LifeItemCat.Gather, 0, false, "UI_Goods_Shirt", null, 0, 1, -6, 0, false, true, false),
            D("gath_mushroom", "버섯", "Mushroom", "나무 그늘에서 딴 버섯. 볶음으로 조리.", "Picked in the shade. Cook into a side.",
                LifeItemCat.Gather, 0, false, "UI_Goods_Side", "dish_veg", 0, 0, 0, 0, false, false, false),
            D("bug_butterfly", "나비", "Butterfly", "잠자리채로 잡은 나비. 도감용.", "Caught with the net. For the collection.",
                LifeItemCat.Gather, 0, false, "Icon_Book", null, 0, 0, 0, 0, false, false, false),
            D("bug_dragonfly", "잠자리", "Dragonfly", "물가를 쌩 날아다닌다. 잡기 어렵다.", "Zips around the water. Hard to catch.",
                LifeItemCat.Gather, 0, false, "Icon_Book", null, 0, 0, 0, 0, false, false, false),
            D("bug_ladybug", "무당벌레", "Ladybug", "꽃밭 풀잎에 앉아 있다. 행운의 벌레.", "Sits on the flowers. Lucky bug.",
                LifeItemCat.Gather, 0, false, "Icon_Book", null, 0, 0, 0, 0, false, false, false),

            // ── 옷 ──
            D("clothes_set", "새 옷 세트", "New Clothes Set", "3개월(12주) 입는 옷. 낡으면 컨디션↓", "Lasts 12 weeks. Worn = condition ↓",
                LifeItemCat.Clothes, Survival.ClothesPrice, true, "UI_Goods_Shirt", null, 0, 0, 0, 0, false, true, true),
        };

        private static LifeItemDef D(string id, string ko, string en, string bk, string be,
            LifeItemCat cat, int price, bool shop, string art, string cookTo,
            int hunger, int condition, int stress, int stamina, bool edible, bool usable, bool clothes)
        {
            return new LifeItemDef
            {
                id = id, nameKo = ko, nameEn = en, blurbKo = bk, blurbEn = be,
                cat = cat, price = price, shop = shop, art = art, cookTo = cookTo,
                hunger = hunger, condition = condition, stress = stress, stamina = stamina,
                edible = edible, usable = usable, clothesPack = clothes
            };
        }

        public static LifeItemDef? Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++) if (All[i].id == id) return All[i];
            return null;
        }

        public static string Name(LifeItemDef d) => Loc.T(d.nameKo, d.nameEn);
        public static string Blurb(LifeItemDef d) => Loc.T(d.blurbKo, d.blurbEn);

        public static string CatLabel(LifeItemCat c) => c switch
        {
            LifeItemCat.Ingredient => Loc.T("재료", "Ingredients"),
            LifeItemCat.BasicDish => Loc.T("기본요리", "Basic Meals"),
            LifeItemCat.PremiumDish => Loc.T("고급요리", "Premium"),
            LifeItemCat.Medicine => Loc.T("약", "Medicine"),
            LifeItemCat.Care => Loc.T("케어", "Care"),
            LifeItemCat.Clothes => Loc.T("옷·생활", "Clothes"),
            LifeItemCat.Gather => Loc.T("채집", "Gathered"),
            _ => "?"
        };

        public static IEnumerable<LifeItemDef> ShopOf(LifeItemCat cat)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].shop && All[i].cat == cat) yield return All[i];
        }

        // ── 인벤토리 ──

        public static void Ensure(SaveData s)
        {
            if (s == null) return;
            if (s.bag == null) s.bag = Array.Empty<LifeStack>();
            if (s.invMigrated) return;
            s.invMigrated = true;
            // 옛 쌀/반찬 주분 → 재료 + 바로 먹을 수 있는 기본요리 약간
            if (s.rice > 0)
            {
                Add(s, "ing_rice", s.rice);
                Add(s, "dish_rice", Mathf.Min(2, s.rice)); // 시작 직 먹을 흰 확보
                s.rice = 0;
            }
            if (s.sideDish > 0)
            {
                Add(s, "ing_veg", s.sideDish);
                Add(s, "dish_veg", Mathf.Min(2, s.sideDish));
                s.sideDish = 0;
            }
            if (Count(s, "dish_rice") <= 0 && Count(s, "ing_rice") <= 0)
                Add(s, "dish_rice", 2); // 완전 빈 세이브 안전망
        }

        public static int Count(SaveData s, string id)
        {
            if (s?.bag == null || string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < s.bag.Length; i++)
                if (s.bag[i] != null && s.bag[i].id == id) return Mathf.Max(0, s.bag[i].n);
            return 0;
        }

        public static int CountCat(SaveData s, LifeItemCat cat)
        {
            Ensure(s); int n = 0;
            if (s.bag == null) return 0;
            for (int i = 0; i < s.bag.Length; i++)
            {
                var st = s.bag[i]; if (st == null || st.n <= 0) continue;
                var d = Get(st.id); if (d.HasValue && d.Value.cat == cat) n += st.n;
            }
            return n;
        }

        public static int TotalOwned(SaveData s)
        {
            Ensure(s); int n = 0;
            if (s.bag == null) return 0;
            for (int i = 0; i < s.bag.Length; i++) if (s.bag[i] != null) n += Mathf.Max(0, s.bag[i].n);
            return n;
        }

        public static void Add(SaveData s, string id, int n = 1)
        {
            if (s == null || string.IsNullOrEmpty(id) || n <= 0) return;
            if (s.bag == null) s.bag = Array.Empty<LifeStack>();
            for (int i = 0; i < s.bag.Length; i++)
            {
                if (s.bag[i] != null && s.bag[i].id == id) { s.bag[i].n += n; SyncLegacy(s); return; }
            }
            var list = new List<LifeStack>(s.bag) { new LifeStack { id = id, n = n } };
            s.bag = list.ToArray();
            SyncLegacy(s);
        }

        public static bool Take(SaveData s, string id, int n = 1)
        {
            if (s == null || n <= 0 || Count(s, id) < n) return false;
            for (int i = 0; i < s.bag.Length; i++)
            {
                if (s.bag[i] == null || s.bag[i].id != id) continue;
                s.bag[i].n -= n;
                if (s.bag[i].n <= 0)
                {
                    var list = new List<LifeStack>(s.bag);
                    list.RemoveAt(i);
                    s.bag = list.ToArray();
                }
                SyncLegacy(s);
                return true;
            }
            return false;
        }

        /// UI 호환: rice/sideDish 를 재료+요리 합으로 미러.
        public static void SyncLegacy(SaveData s)
        {
            if (s == null) return;
            s.rice = Count(s, "ing_rice") + Count(s, "dish_rice") + Count(s, "dish_egg") + Count(s, "dish_bento");
            s.sideDish = Count(s, "ing_veg") + Count(s, "ing_meat") + Count(s, "ing_fish")
                       + Count(s, "dish_veg") + Count(s, "dish_meat") + Count(s, "dish_fish")
                       + Count(s, "dish_premium_stew") + Count(s, "dish_soup");
        }

        public static bool CanBuy(SaveData s, string id, int qty)
        {
            var d = Get(id); if (!d.HasValue || !d.Value.shop || qty <= 0 || s == null) return false;
            long cost = (long)d.Value.price * qty;
            return s.stats.money >= cost;
        }

        public static bool Buy(SaveData s, string id, int qty)
        {
            if (!CanBuy(s, id, qty)) return false;
            var d = Get(id).Value;
            s.stats.money -= d.price * qty;
            if (d.clothesPack)
            {
                // 옷은 인벤에 넣지 않고 바로 입음(여러 벌 사면 주수만 연장)
                s.clothesWeeks = Mathf.Max(s.clothesWeeks, 0) + Survival.ClothesWeeks * qty;
                return true;
            }
            Add(s, id, qty);
            return true;
        }

        public static bool CanCook(SaveData s, string ingId)
        {
            var d = Get(ingId);
            return d.HasValue && d.Value.cat == LifeItemCat.Ingredient && !string.IsNullOrEmpty(d.Value.cookTo) && Count(s, ingId) > 0;
        }

        public static bool Cook(SaveData s, string ingId, int qty = 1)
        {
            var d = Get(ingId);
            if (!d.HasValue || string.IsNullOrEmpty(d.Value.cookTo) || qty <= 0) return false;
            if (!Take(s, ingId, qty)) return false;
            // 양념은 고급요리로(재료 1→요리 1)
            Add(s, d.Value.cookTo, qty);
            return true;
        }

        public static List<(LifeItemDef def, int n)> ListOwned(SaveData s, LifeItemCat? only = null)
        {
            Ensure(s);
            var r = new List<(LifeItemDef, int)>();
            if (s.bag == null) return r;
            for (int i = 0; i < s.bag.Length; i++)
            {
                var st = s.bag[i]; if (st == null || st.n <= 0) continue;
                var d = Get(st.id); if (!d.HasValue) continue;
                if (only.HasValue && d.Value.cat != only.Value) continue;
                r.Add((d.Value, st.n));
            }
            r.Sort((a, b) => ((int)a.Item1.cat).CompareTo((int)b.Item1.cat));
            return r;
        }

        public static List<(LifeItemDef def, int n)> ListEdible(SaveData s)
        {
            Ensure(s);
            var r = new List<(LifeItemDef, int)>();
            if (s.bag == null) return r;
            for (int i = 0; i < s.bag.Length; i++)
            {
                var st = s.bag[i]; if (st == null || st.n <= 0) continue;
                var d = Get(st.id); if (!d.HasValue || !d.Value.edible) continue;
                r.Add((d.Value, st.n));
            }
            return r;
        }

        public static bool HasEdible(SaveData s) => ListEdible(s).Count > 0;

        /// 식사 — 요리 1개 소진 + 효과. 성공 시 ateThisWeek.
        public static bool Eat(SaveData s, string id)
        {
            var d = Get(id);
            if (!d.HasValue || !d.Value.edible) return false;
            if (!Take(s, id, 1)) return false;
            ApplyEffects(s, d.Value);
            s.ateThisWeek = true;
            s.starveWeeks = 0;
            return true;
        }

        public static bool Use(SaveData s, string id)
        {
            var d = Get(id);
            if (!d.HasValue || !d.Value.usable) return false;
            if (d.Value.clothesPack)
            {
                // 인벤에 옷이 있다면(혹시 Add된 경우)
                if (Count(s, id) > 0 && !Take(s, id, 1)) return false;
                s.clothesWeeks = Survival.ClothesWeeks;
                return true;
            }
            if (!Take(s, id, 1)) return false;
            ApplyEffects(s, d.Value);
            if (d.Value.cat == LifeItemCat.Care && d.Value.id == "care_perfume")
                s.stats.charm = Mathf.Min(PlayerStats.StatMax, s.stats.charm + 1);
            if (d.Value.id == "care_book")
                s.stats.sense = Mathf.Min(PlayerStats.StatMax, s.stats.sense + 1);
            return true;
        }

        private static void ApplyEffects(SaveData s, LifeItemDef d)
        {
            s.hunger = Mathf.Clamp(s.hunger + d.hunger, 0, 100);
            s.condition = Mathf.Clamp(s.condition + d.condition, 0, 100);
            s.stats.stress = Mathf.Clamp(s.stats.stress + d.stress, 0, PlayerStats.StatMax);
            s.stats.stamina = Mathf.Clamp(s.stats.stamina + d.stamina, 0, PlayerStats.StatMax);
        }

        public static string EffectText(LifeItemDef d)
        {
            var parts = new List<string>();
            if (d.hunger != 0) parts.Add(Loc.T($"배부름{d.hunger:+#;-#}", $"Full{d.hunger:+#;-#}"));
            if (d.condition != 0) parts.Add(Loc.T($"컨디션{d.condition:+#;-#}", $"Cond{d.condition:+#;-#}"));
            if (d.stress != 0) parts.Add(Loc.T($"스트레스{d.stress:+#;-#}", $"Stress{d.stress:+#;-#}"));
            if (d.stamina != 0) parts.Add(Loc.T($"체력{d.stamina:+#;-#}", $"Sta{d.stamina:+#;-#}"));
            if (d.clothesPack) parts.Add(Loc.T("+12주 옷", "+12w clothes"));
            if (!string.IsNullOrEmpty(d.cookTo))
            {
                var to = Get(d.cookTo);
                if (to.HasValue) parts.Add(Loc.T($"조리→{to.Value.nameKo}", $"Cook→{to.Value.nameEn}"));
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : "";
        }
    }
}
