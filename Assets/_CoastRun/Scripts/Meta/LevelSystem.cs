using UnityEngine;

namespace CoastRun
{
    /// 53차(사용자): 육성 **레벨·경험치**. 젤리는 경험치로(통화 아님), 행동·러닝·미니게임·이야기가 경험치를 준다.
    ///   레벨업마다 스탯이 조금씩 오르고(체력 +2·순발력 +1·매력 +1·감성 +1), 러닝 코인 +1 %/Lv(최대 +30 %).
    ///   롱컷씬은 레벨이 되어야 열린다(LevelForLongCut) — 롱컷이 K-POP 11챕터+를 여니 레벨이 「킥」.
    ///   필요 경험치 Need(L) = 80 + 40·L (Lv1→2 120, Lv10→11 480, Lv20→21 880). 상한 MaxLevel.
    public static class LevelSystem
    {
        public const int MaxLevel = 40;
        // 경험치 표
        public const int ExpJelly = 5, ExpBigJelly = 15, ExpAction = 10, ExpActionGreat = 20, ExpActionFail = 4;
        public const int ExpStoryRun = 120, ExpKpopFinish = 60, ExpBoss = 20, ExpMinigame = 40, ExpChapterRead = 50, ExpRubMax = 2;

        public static int Need(int level) => 80 + 40 * Mathf.Max(1, level);

        private static SaveData S => GameManager.I != null ? GameManager.I.Save : null;
        public static int Level => S != null ? Mathf.Max(1, S.level) : 1;
        public static int Exp => S != null ? S.exp : 0;
        public static float Progress01 => S == null ? 0f : Mathf.Clamp01(S.exp / (float)Need(Level));

        /// 경험치 추가(레벨업 처리·토스트). 세이브가 없으면(순수 K-POP 러닝만 하는 유저) PlayerPrefs 에 쌓아 두었다가 세이브가 생기면 합친다.
        public static void Add(int amount, string reasonKo = null, string reasonEn = null)
        {
            if (amount <= 0) return;
            var s = S;
            if (s == null) { PlayerPrefs.SetInt(PendingKey, PlayerPrefs.GetInt(PendingKey, 0) + amount); return; }
            s.exp += amount;
            int ups = 0;
            while (s.level < MaxLevel && s.exp >= Need(s.level)) { s.exp -= Need(s.level); s.level++; ups++; OnLevelUp(s); }
            if (s.level >= MaxLevel) s.exp = Mathf.Min(s.exp, Need(s.level) - 1);
            if (ups > 0)
            {
                CoastToast.Show(Loc.T($"레벨 업! Lv {s.level}  · 체력 +{2 * ups} 순발력 +{ups} 매력 +{ups}", $"LEVEL UP! Lv {s.level}"));
                CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.7f);
                if (GameManager.I != null) GameManager.I.Persist();
            }
        }
        public const string PendingKey = "CoastRun.PendingExp";
        /// 세이브 로드/새 회차 때 — 세이브 없이 모은 경험치 합치기.
        public static void FlushPending()
        {
            int p = PlayerPrefs.GetInt(PendingKey, 0);
            if (p <= 0 || S == null) return;
            PlayerPrefs.SetInt(PendingKey, 0);
            Add(p);
        }

        private static void OnLevelUp(SaveData s)
        {
            var st = s.stats;
            st.stamina = Mathf.Min(PlayerStats.StatMax, st.stamina + 2);
            st.agility = Mathf.Min(PlayerStats.StatMax, st.agility + 1);
            st.charm = Mathf.Min(PlayerStats.StatMax, st.charm + 1);
            st.sense = Mathf.Min(PlayerStats.StatMax, st.sense + 1);
        }

        /// 러닝 코인 배수: +1 %/Lv, 최대 +30 %.
        public static float CoinMul(SaveData s) => 1f + Mathf.Min(0.30f, 0.01f * ((s != null ? Mathf.Max(1, s.level) : 1) - 1));

        /// 롱컷씬 레벨 조건: CH4 Lv3 · CH7 Lv5 · CH10 Lv7 · CH13 Lv9 · CH15(마지막 롱컷) Lv12 · 엔딩(CH20) Lv15.
        public static int LevelForLongCut(int chapter)
        {
            switch (chapter)
            {
                case 4: return 3;
                case 7: return 5;
                case 10: return 7;
                case 13: return 9;
                case 15: return 12;
                case 20: return 15;
                default: return 1;
            }
        }
        public static bool LongCutOpen(int chapter) => Level >= LevelForLongCut(chapter) || (GameManager.I != null && GameManager.I.DevUnlockAll);

        /// 다음에 열리는 롱컷(레벨 부족한 것 중 가장 낮은) — 상태창 힌트.
        public static string NextUnlockHint()
        {
            int[] chs = { 4, 7, 10, 13, 15, 20 };
            string[] ko = { "롱컷 「하트」", "롱컷 「우리 기지」", "롱컷 「열두 개의 초」", "롱컷 「그 밤」", "롱컷 「스무 살」", "엔딩" };
            string[] en = { "long cut 'Heart'", "long cut 'Our Base'", "long cut 'Twelve Candles'", "long cut 'That Night'", "long cut 'Twenty'", "the ending" };
            for (int i = 0; i < chs.Length; i++)
                if (Level < LevelForLongCut(chs[i])) return Loc.T($"Lv {LevelForLongCut(chs[i])} 에 {ko[i]} 열림", $"Lv {LevelForLongCut(chs[i])} opens {en[i]}");
            return Loc.T("모든 롱컷이 레벨 조건을 넘었어요", "All long cuts are level-ready");
        }

        /// 칭호(5레벨마다).
        public static string Title(int level)
        {
            if (level >= 30) return Loc.T("주파수의 주인", "Master of Frequency");
            if (level >= 25) return Loc.T("송전탑 러너", "Tower Runner");
            if (level >= 20) return Loc.T("해안도로 스타", "Coast Road Star");
            if (level >= 15) return Loc.T("스무 살 준비", "Ready for Twenty");
            if (level >= 10) return Loc.T("제주 소녀", "Jeju Girl");
            if (level >= 5) return Loc.T("초보 러너", "Rookie Runner");
            return Loc.T("새내기", "Newcomer");
        }

        /// 1000 단위는 k, 1,000,000 단위는 M — 육성 화면 돈·코인 표기(53차).
        public static string FormatK(long n)
        {
            if (n >= 1000000) return (n / 1000000f).ToString(n >= 10000000 ? "0" : "0.0") + "M";
            if (n >= 1000) return (n / 1000f).ToString(n >= 100000 ? "0" : "0.0") + "k";
            return n.ToString();
        }
    }
}
