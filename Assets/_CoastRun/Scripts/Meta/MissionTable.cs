using UnityEngine;

namespace CoastRun
{
    /// 챕터 미션 — 챕터마다 별 3개: ★1 클리어, ★2·★3 조건. 총 60별. 별 수로 팬아트가 열린다.
    public enum MissionKind { NoHit, NearMiss, Coins, Combo, Hearts, MaxHits, Fast }

    public struct MissionDef
    {
        public MissionKind kind; public int value;
        public MissionDef(MissionKind k, int v) { kind = k; value = v; }

        public string Text
        {
            get
            {
                switch (kind)
                {
                    case MissionKind.NoHit: return Loc.T("무피격으로 도착", "Arrive without a hit");
                    case MissionKind.NearMiss: return Loc.T($"니어미스 {value}회", $"{value} near misses");
                    case MissionKind.Coins: return Loc.T($"코인 {value}개", $"{value} coins");
                    case MissionKind.Combo: return Loc.T($"니어미스 콤보 {value}", $"Near-miss combo {value}");
                    case MissionKind.Hearts: return Loc.T($"하트 {value}개", $"{value} hearts");
                    case MissionKind.MaxHits: return Loc.T($"피격 {value}회 이하", $"At most {value} hits");
                    default: return Loc.T($"{value}초 안에 도착", $"Arrive within {value}s");
                }
            }
        }

        public bool Check(StageRunStats s)
        {
            if (s == null) return false;
            switch (kind)
            {
                case MissionKind.NoHit: return s.SoftHits == 0;
                case MissionKind.NearMiss: return s.NearMissCount >= value;
                case MissionKind.Coins: return s.Coins >= value;
                case MissionKind.Combo: return s.BestCombo >= value;
                case MissionKind.Hearts: return s.Hearts >= value;
                case MissionKind.MaxHits: return s.SoftHits <= value;
                default: return s.Seconds <= value;
            }
        }
    }

    public static class MissionTable
    {
        static MissionDef M(MissionKind k, int v) => new MissionDef(k, v);

        /// [챕터-1][0..1] — 봄은 쉽게, 겨울로 갈수록 빡빡하게.
        public static readonly MissionDef[][] Missions =
        {
            new[] { M(MissionKind.Coins, 40),    M(MissionKind.MaxHits, 3) },   // 1
            new[] { M(MissionKind.NearMiss, 6),  M(MissionKind.Hearts, 20) },   // 2
            new[] { M(MissionKind.Coins, 60),    M(MissionKind.Combo, 3) },     // 3
            new[] { M(MissionKind.NearMiss, 10), M(MissionKind.MaxHits, 2) },   // 4
            new[] { M(MissionKind.Hearts, 24),   M(MissionKind.NoHit, 0) },     // 5
            new[] { M(MissionKind.Coins, 80),    M(MissionKind.NearMiss, 12) }, // 6
            new[] { M(MissionKind.Combo, 4),     M(MissionKind.MaxHits, 2) },   // 7
            new[] { M(MissionKind.Hearts, 25),   M(MissionKind.NearMiss, 14) }, // 8
            new[] { M(MissionKind.Coins, 90),    M(MissionKind.NoHit, 0) },     // 9
            new[] { M(MissionKind.Combo, 5),     M(MissionKind.Hearts, 26) },   // 10
            new[] { M(MissionKind.NearMiss, 16), M(MissionKind.MaxHits, 1) },   // 11
            new[] { M(MissionKind.Coins, 100),   M(MissionKind.Combo, 5) },     // 12
            new[] { M(MissionKind.Hearts, 27),   M(MissionKind.NoHit, 0) },     // 13
            new[] { M(MissionKind.NearMiss, 18), M(MissionKind.Coins, 110) },   // 14
            new[] { M(MissionKind.Combo, 6),     M(MissionKind.MaxHits, 1) },   // 15
            new[] { M(MissionKind.Hearts, 27),   M(MissionKind.NearMiss, 20) }, // 16
            new[] { M(MissionKind.Coins, 120),   M(MissionKind.NoHit, 0) },     // 17
            new[] { M(MissionKind.Combo, 7),     M(MissionKind.Hearts, 28) },   // 18
            new[] { M(MissionKind.NearMiss, 22), M(MissionKind.MaxHits, 1) },   // 19
            new[] { M(MissionKind.NoHit, 0),     M(MissionKind.Hearts, 30) },   // 20
        };

        public static MissionDef Get(int chapter, int slot) => Missions[Mathf.Clamp(chapter, 1, 20) - 1][Mathf.Clamp(slot, 0, 1)];

        public static int Stars(MetaProfile p, int chapter)
        {
            if (p == null) return 0;
            int m = p.starMask[Mathf.Clamp(chapter, 1, 20) - 1];
            return (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1);
        }

        public static bool Has(MetaProfile p, int chapter, int bit) => p != null && (p.starMask[Mathf.Clamp(chapter, 1, 20) - 1] & (1 << bit)) != 0;

        /// 런 정산 때 호출. 새로 딴 별 수를 돌려준다.
        public static int Settle(MetaProfile p, int chapter, StageRunStats s)
        {
            if (p == null) return 0;
            int i = Mathf.Clamp(chapter, 1, 20) - 1;
            int before = p.starMask[i];
            int mask = before | 1;
            if (Get(chapter, 0).Check(s)) mask |= 2;
            if (Get(chapter, 1).Check(s)) mask |= 4;
            p.starMask[i] = mask;
            int gained = 0;
            for (int b = 0; b < 3; b++) if ((mask & (1 << b)) != 0 && (before & (1 << b)) == 0) gained++;
            return gained;
        }

        /// 별로 열리는 팬아트 장수(15별당 1장).
        public static int FanArtUnlocked(MetaProfile p) => p == null ? 0 : Mathf.Min(4, p.StarsTotal / 15);
        public const int StarsPerFanArt = 15;
    }
}
