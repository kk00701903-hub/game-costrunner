using System.Text.RegularExpressions;
using UnityEngine;

namespace CoastRun
{
    /// 대본 조건 태그 — 같은 컷씬이 회차마다 다르게 읽히게 한다.
    ///   SAY 화자 칸:  하늘[감성>=50]     NARR/LETTER 본문 앞:  [평판<20]지문…
    ///   이름: 체력 순발력 매력 감성 평판 스트레스 골칫거리 돈 하트 회차 루아 만수 할머니 DJ  (영문도 허용)
    ///   연산: >= > <= < == !=   여러 조건은 & 로 (예: [회차>=2&감성>=40])
    /// 세이브가 없으면(회상·에디터 미리보기) 조건 줄은 보인다.
    public static class StoryCond
    {
        static readonly Regex Tag = new Regex(@"\[([^\]]+)\]");

        /// 태그를 떼어낸 문자열을 돌려주고, 조건 결과를 out 으로.
        public static string Strip(string s, out bool pass)
        {
            pass = true;
            if (string.IsNullOrEmpty(s) || s.IndexOf('[') < 0) return s;
            var m = Tag.Match(s);
            if (!m.Success) return s;
            pass = Eval(m.Groups[1].Value);
            return s.Remove(m.Index, m.Length).Trim();
        }

        public static bool Eval(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return true;
            foreach (var part in expr.Split('&'))
                if (!EvalOne(part.Trim())) return false;
            return true;
        }

        static bool EvalOne(string e)
        {
            var m = Regex.Match(e, @"^([^<>=!]+)\s*(>=|<=|==|!=|>|<)\s*(-?\d+)$");
            if (!m.Success) return true;
            int v = Value(m.Groups[1].Value.Trim());
            int n = int.Parse(m.Groups[3].Value);
            switch (m.Groups[2].Value)
            {
                case ">=": return v >= n;
                case "<=": return v <= n;
                case "==": return v == n;
                case "!=": return v != n;
                case ">": return v > n;
                default: return v < n;
            }
        }

        public static int Value(string name)
        {
            var gm = GameManager.I;
            var s = gm != null ? gm.Save : null;
            var st = s != null ? s.stats : null;
            switch (name.ToLowerInvariant())
            {
                case "체력": case "stamina": return st?.stamina ?? 999;
                case "게이트": case "gate": return s != null ? StoryGate.Margin(s) : 0;          // 26차: 체력 − 요구치(≥0 통과)
                case "필요체력": case "need": return s != null ? StoryGate.Required(s) : 0;
                case "순발력": case "agility": return st?.agility ?? 999;
                case "매력": case "charm": return st?.charm ?? 999;
                case "감성": case "sense": return st?.sense ?? 999;
                case "평판": case "trust": return st?.trust ?? 999;
                case "스트레스": case "stress": return st?.stress ?? 0;
                case "골칫거리": case "말썽": case "trouble": return st?.trouble ?? 0;
                case "돈": case "money": return st?.money ?? 999;
                case "하트": case "hearts": return s?.chapterHearts ?? 999;
                case "회차": case "playthrough": case "ng": return s?.playthrough ?? 1;
                case "늦음": case "late": return s != null ? (s.lastRunLate ? 1 : 0) : 0;
                case "늦은날": case "laterun": case "lateruns": return s?.lateRuns ?? 0;
                case "루아": case "rua": return Affinity.Get(s, Affinity.Rua);
                case "만수": case "mansu": return Affinity.Get(s, Affinity.Mansu);
                case "할머니": case "grandma": return Affinity.Get(s, Affinity.Grandma);
                case "dj": case "디제이": return Affinity.Get(s, Affinity.DJ);
                default: return 0;
            }
        }
    }

    /// NPC 호감도 4인 — 스케줄로 오르고, 문턱마다 사이드 씬(SIDE_<NPC>_<n>).
    public static class Affinity
    {
        public const int Rua = 0, Mansu = 1, Grandma = 2, DJ = 3;
        public static readonly int[] Thresholds = { 3, 7, 12 };
        public static readonly string[] Ids = { "RUA", "MANSU", "GRANDMA", "DJ" };

        public static string Name(int npc)
        {
            switch (npc) { case Rua: return Loc.T("루아", "Rua"); case Mansu: return Loc.T("만수", "Mansu"); case Grandma: return Loc.T("할머니", "Grandma"); default: return "DJ"; }
        }

        public static int Get(SaveData s, int npc) => s != null && s.affinity != null && npc < s.affinity.Length ? s.affinity[npc] : 0;
        public static int Level(SaveData s, int npc) { int v = Get(s, npc); int l = 0; foreach (var t in Thresholds) if (v >= t) l++; return l; }

        /// 스케줄 id → (npc, 기본 가중치). 없으면 -1.
        public static int NpcOf(string id)
        {
            switch (id)
            {
                case "job_market": case "job_sashimi": case "job_delivery": case "job_night_delivery": return Mansu;
                case "job_orange": case "job_hall": case "job_dangsan": case "les_cook": case "les_speech": return Grandma;
                case "dev_radio": case "les_ham": case "job_dj_assist": return DJ;
                case "dev_oreum": case "job_lighthouse": case "rest_sea": case "les_photo": case "job_tower_fix": return Rua;
                default: return -1;
            }
        }

        /// 스케줄 결과 반영. 새로 넘은 문턱이 있으면 그 씬 id, 없으면 null.
        public static string OnSchedule(SaveData s, string id, Outcome outcome)
        {
            if (s == null) return null;
            if (s.affinity == null || s.affinity.Length < 4) s.affinity = new int[4];
            int npc = NpcOf(id);
            if (npc < 0 || outcome == Outcome.Fail) return null;
            int before = Level(s, npc);
            s.affinity[npc] += outcome == Outcome.GreatSuccess ? 2 : 1;
            int after = Level(s, npc);
            if (after > before)
            {
                int bit = 1 << (npc * 3 + after - 1);
                if ((s.affinityShown & bit) == 0) { s.affinityShown |= bit; return $"SIDE_{Ids[npc]}_{after}"; }
            }
            return null;
        }

        /// 문턱 보상(씬이 열릴 때 한 번).
        public static void Reward(SaveData s, int level)
        {
            if (s == null) return;
            if (level == 1) s.stats.money += 60;
            else if (level == 2) { s.stats.trust += 4; s.stats.sense += 2; }
            else { s.stats.trust += 6; s.chapterHearts += 4; }
            s.stats.Clamp();
        }
    }
}
