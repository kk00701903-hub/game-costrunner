using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 219차: 언어팩 자체 점검(에디터 전용). ja/es 로 잠깐 바꿔 번역표·틀 번역·스토리 영어를 찍어 보고 원래 언어로 되돌린다.
    public static class LocSelfTest219
    {
        [MenuItem("Coast Run/Store/219 - Loc self test")]
        public static void Run()
        {
            string saved = PlayerPrefs.GetString(Loc.PrefKey, "");
            try
            {
                foreach (var lang in new[] { "en", "ja", "es" })
                {
                    Loc.SetLang(lang);
                    Debug.Log($"[LocTest] {lang} | {Loc.T("설정", "Settings")} | {Loc.Tr("Not enough coins — need 250G.")} | {Loc.Tr("🥚 Collected 3 eggs! (5 in bag)")} | {StoryEn.T("눈을 떴다. 낡은 담요 위였다. 바람이 담요 끝을 자꾸 들췄다.")} | {ChapterScript.Title(1)} | {StoryEn.T("— 회상 —")}");
                }
            }
            finally
            {
                if (string.IsNullOrEmpty(saved)) { PlayerPrefs.DeleteKey(Loc.PrefKey); PlayerPrefs.Save(); } else Loc.SetLang(saved);
                Debug.Log("[LocTest] restored lang pref = '" + saved + "'");
            }
        }
    }
}
