using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 171차(사용자: 「지나다니는 사람들이 일주일에 2~3개 정도 미션 주도록 — 토끼 잡기, 물고기 잡기 등」).
    /// 주차마다 마을 사람 2~3명이 부탁을 하나씩 갖는다(주차 시드로 결정). 대화 버튼으로 받고(📜), 마을에서 목표를 채우면(VillageMission.Kind 진행) 다시 말 걸어 보상.
    /// 일일 미션(VillageMission)과 달리 활동 칸을 쓰지 않는다. 데이터: SaveData.reqWeek/reqNpc/reqKind/reqProg/reqState(0 제안·1 받음·2 완료·3 보상 받음).
    public static class VillageRequest
    {
        static readonly VillageMission.Kind[] Pool = { VillageMission.Kind.Rabbit, VillageMission.Kind.Crab, VillageMission.Kind.Fish, VillageMission.Kind.Bug, VillageMission.Kind.Egg, VillageMission.Kind.Chop, VillageMission.Kind.Pick, VillageMission.Kind.Bandit, VillageMission.Kind.Mine };
        public const int NpcCount = 6;

        public static void Ensure(SaveData s)
        {
            if (s == null) return;
            if (s.reqKind == null || s.reqWeek != s.week)
            {
                var rng = new System.Random(s.week * 131 + s.seed * 17 + 171);
                int n = 2 + (rng.NextDouble() < 0.5 ? 1 : 0);
                var npcs = new List<int>(); while (npcs.Count < n) { int i = rng.Next(NpcCount); if (!npcs.Contains(i)) npcs.Add(i); }
                s.reqWeek = s.week; s.reqNpc = new int[n]; s.reqKind = new int[n]; s.reqProg = new int[n]; s.reqState = new int[n];
                var used = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    int k; do { k = (int)Pool[rng.Next(Pool.Length)]; } while (used.Contains(k)); used.Add(k);
                    s.reqNpc[i] = npcs[i]; s.reqKind[i] = k;
                }
            }
        }
        static int IndexFor(SaveData s, int npc) { Ensure(s); if (s == null) return -1; for (int i = 0; i < s.reqNpc.Length; i++) if (s.reqNpc[i] == npc) return i; return -1; }
        /// NPC 머리 위 표시용: 0 없음 · 1 제안 있음(📜) · 2 완료(🎁) · 3 진행 중
        public static int MarkFor(SaveData s, int npc) { int i = IndexFor(s, npc); if (i < 0) return 0; int st = s.reqState[i]; return st == 0 ? 1 : st == 2 ? 2 : st == 1 ? 3 : 0; }
        public static int Goal(SaveData s, int i) => VillageMission.Goal((VillageMission.Kind)s.reqKind[i]);
        public static int Reward(VillageMission.Kind k) => VillageMission.Money(k) + 40;

        /// 목표 진행(VillageHub.MissionTick 이 종류마다 부른다)
        public static void Tick(VillageHub hub, VillageMission.Kind k, int n)
        {
            var s = hub != null ? hub.SaveRef : null; if (s == null) return; Ensure(s);
            for (int i = 0; i < s.reqKind.Length; i++)
            {
                if (s.reqState[i] != 1 || s.reqKind[i] != (int)k) continue;
                int goal = Goal(s, i); s.reqProg[i] = Mathf.Min(goal, s.reqProg[i] + n);
                string who = hub.NpcName(s.reqNpc[i]);
                if (s.reqProg[i] >= goal) { s.reqState[i] = 2; CoastToast.Show(Loc.T($"📜 {who}의 부탁 완료! 가서 말을 걸자 — {VillageMission.Title(k)}", $"📜 {who}'s request done! Go talk — {VillageMission.Title(k)}")); }
                else CoastToast.Show(Loc.T($"📜 {who}의 부탁: {VillageMission.Title(k)} ({s.reqProg[i]}/{goal})", $"📜 {who}: {VillageMission.Title(k)} ({s.reqProg[i]}/{goal})"));
                hub.PersistRefresh();
            }
        }

        /// 대화 버튼: 이 NPC 에게 부탁이 있으면 처리하고 true
        public static bool OnTalk(VillageHub hub, int npc)
        {
            var s = hub != null ? hub.SaveRef : null; if (s == null) return false;
            int i = IndexFor(s, npc); if (i < 0) return false;
            var k = (VillageMission.Kind)s.reqKind[i]; string who = hub.NpcName(npc); int goal = Goal(s, i);
            switch (s.reqState[i])
            {
                case 0:
                    hub.NpcSay(npc, Loc.T($"저기… 부탁이 하나 있는데. {VillageMission.Title(k)} 해 줄 수 있어?", $"Um… could you {VillageMission.Title(k)}?"));
                    hub.ShowChoice(Loc.T($"📜 {who}의 부탁", $"📜 {who}'s request"), Loc.T($"{VillageMission.Title(k)} — {VillageMission.Hint(k)}. 보상 {Reward(k)}G · 별조각 3. 이번 주 안에.", $"{VillageMission.Title(k)} — {VillageMission.Hint(k)}. Reward {Reward(k)}G · 3 shards. This week."), new[]
                    {
                        (Loc.T("좋아, 해 볼게!", "Sure, I'll do it!"), new Color(0.45f, 0.78f, 0.55f), (Action)(() => { s.reqState[i] = 1; hub.NpcSay(npc, Loc.T("고마워! 다 되면 나한테 와.", "Thanks! Come back when it's done.")); hub.PersistRefresh(); })),
                        (Loc.T("지금은 어려워", "Not right now"), new Color(0.6f, 0.6f, 0.65f), (Action)(() => hub.NpcSay(npc, Loc.T("그래… 생각나면 또 말 걸어 줘.", "Okay… ask me again if you change your mind.")))),
                    });
                    return true;
                case 1:
                    hub.NpcSay(npc, Loc.T($"{VillageMission.Title(k)} — 지금 {s.reqProg[i]}/{goal}. 부탁해!", $"{VillageMission.Title(k)} — {s.reqProg[i]}/{goal} so far. Please!"));
                    return true;
                case 2:
                {
                    int money = Reward(k); s.stats.money += money; s.starShards += 3; s.starShardsTotal += 3; s.reqState[i] = 3;
                    string gift = GiftFor(k); string giftKo = "";
                    if (gift != null) { LifeItems.Add(s, gift, 1); var d = LifeItems.Get(gift); giftKo = d.HasValue ? " · " + LifeItems.Name(d.Value) : ""; }
                    hub.NpcSay(npc, Loc.T("와, 정말 고마워! 이거 받아.", "Wow, thank you! Take this."));
                    CoastToast.Show(Loc.T($"🎁 부탁 보상 +{money}G · 별조각 +3{giftKo}", $"🎁 Reward +{money}G · shards +3{giftKo}"));
                    CoastAudioManager.PlayAnywhere(CoastSfx.Coin); hub.PersistRefresh();
                    return true;
                }
                default:
                    hub.NpcSay(npc, Loc.T("아까는 고마웠어. 다음 주에 또 부탁할지도!", "Thanks for earlier. Maybe another favor next week!"));
                    return true;
            }
        }
        static string GiftFor(VillageMission.Kind k)
        {
            switch (k) { case VillageMission.Kind.Fish: return "ing_spice"; case VillageMission.Kind.Rabbit: return "ing_veg"; case VillageMission.Kind.Egg: return "ing_milk"; case VillageMission.Kind.Bandit: return "ing_meat"; default: return null; }
        }
        /// HUD/메뉴용 요약 줄
        public static string Summary(SaveData s)
        {
            Ensure(s); if (s == null || s.reqKind.Length == 0) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.reqKind.Length; i++) { var k = (VillageMission.Kind)s.reqKind[i]; string st = s.reqState[i] == 0 ? Loc.T("제안", "offer") : s.reqState[i] == 1 ? $"{s.reqProg[i]}/{Goal(s, i)}" : s.reqState[i] == 2 ? Loc.T("완료·보상", "done") : Loc.T("끝", "claimed"); sb.Append(i > 0 ? "\n" : "").Append($"📜 {VillageMission.Title(k)} — {st}"); }
            return sb.ToString();
        }
    }
}
