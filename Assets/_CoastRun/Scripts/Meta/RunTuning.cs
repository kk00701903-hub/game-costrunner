using UnityEngine;

namespace CoastRun
{
    /// 육성 스탯 → 런닝 파라미터. GameManager.Run.Configure(save)가 런 시작 전에 채우고,
    /// 런 스크립트(PlayerController/HealthSystem/CoinPickup/JellySpawner)가 읽는다.
    /// 육성 없이 02_Run을 바로 플레이하면 기본값(러닝 ×1.0, 스탯 30/20/20)으로 돈다.
    public static class RunTuning
    {
        public const int HeartsPerStage = 30;

        public static RunMode Mode = RunMode.Running;
        public static float SpeedMul = 1f;          // 스케이트보드 ×1.3
        public static float CoinMul = 1f;           // 스케이트보드 ×1.3 (펫 배율과 곱연산)
        public static float MaxHp = 100f;           // 100 + 체력
        public static float HitDamage = 14f;        // 체력 ↑ → 감소량 완화
        public static float DashInvincible = 0.8f;  // 순발력 ↑ → 무적 대시 길어짐
        public static float HitFreezeMul = 1f;      // 순발력 ↑ → 피격 경직 단축
        public static float NearMissBonus = 1f;     // 매력 → 니어미스 보너스
        public static bool BurnoutStart;            // 번아웃이면 시작 HP 70%
        public static PetKind Pet = PetKind.None;
        public static bool HasSeason;
        public static SeasonKind Season = SeasonKind.Summer;

        public static void Reset()
        {
            Mode = RunMode.Running;
            SpeedMul = 1f;
            CoinMul = 1f;
            MaxHp = 100f;
            HitDamage = 14f;
            DashInvincible = 0.8f;
            HitFreezeMul = 1f;
            NearMissBonus = 1f;
            BurnoutStart = false;
            Pet = PetKind.None;
            HasSeason = false;
        }

        public static void Configure(SaveData s)
        {
            Reset();
            if (s == null) return;
            var st = s.stats;
            float stamina01 = Mathf.Clamp01(st.stamina / (float)PlayerStats.StatMax);
            float agility01 = Mathf.Clamp01(st.agility / (float)PlayerStats.StatMax);
            float charm01 = Mathf.Clamp01(st.charm / (float)PlayerStats.StatMax);

            MaxHp = 100f + st.stamina * 0.5f;                       // 100 ~ 200
            HitDamage = 14f * (1f - 0.45f * stamina01);             // 14 → 7.7
            DashInvincible = 0.8f + 1.2f * agility01;               // 0.8 ~ 2.0 s
            HitFreezeMul = 1f - 0.5f * agility01;                   // 경직 최대 절반
            NearMissBonus = 1f + charm01;                           // 최대 ×2
            BurnoutStart = st.Burnout;
            Pet = s.equippedPet;
            HasSeason = true;
            Season = Timeline.SeasonOf(s.week);

            Mode = s.runMode;
            bool skate = s.runMode == RunMode.Skateboard;
            SpeedMul = skate ? 1.3f : 1f;
            CoinMul = skate ? 1.3f : 1f;
        }
    }
}
