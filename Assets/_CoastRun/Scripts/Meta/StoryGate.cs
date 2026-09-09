using UnityEngine;

namespace CoastRun
{
    /// 26차: 스토리 모드 게이트 — 육성(체력)이 챕터 요구치에 못 미치면 그 챕터의 러닝에 들어갈 수 없다.
    /// 챕터 경계(마지막 주)에서 오프닝 컷씬이 '이벤트'로 뜨고, 컷씬 안 [게이트>=0]/[게이트<0] 분기로 통과·불통과가 갈린다.
    /// 불통과면 챕터 마감이 1주 늘어나고 육성이 계속된다(Timeline 52주는 뒤로 밀린다).
    public static class StoryGate
    {
        /// 챕터별 요구 체력. 시작 30 → CH1 30(자동 통과) … CH20 144. StatMax 200, 체력 스케줄이 페이즈당 +2~3.
        public static int RequiredStamina(int chapter) => Mathf.Clamp(24 + 6 * Mathf.Clamp(chapter, 1, Timeline.Chapters), 0, PlayerStats.StatMax);

        public static int Stamina(SaveData s) => s?.stats?.stamina ?? 0;
        public static int Required(SaveData s) => s != null ? RequiredStamina(s.chapter) : 0;
        /// 양수면 통과(여유), 음수면 부족분.
        public static int Margin(SaveData s) => Stamina(s) - Required(s);
        public static bool Passes(SaveData s) => s != null && Margin(s) >= 0;

        public static string FailText(SaveData s) =>
            Loc.T($"아직 달릴 수 없어.\n체력 {Stamina(s)} / 필요 {Required(s)}  —  {-Margin(s)}만 더 키우면 돼.\n이번 챕터는 한 주 더 육성할 수 있어.",
                  $"Not ready to run yet.\nStamina {Stamina(s)} / need {Required(s)} — {-Margin(s)} more.\nThis chapter gets one more week of training.");
    }
}
