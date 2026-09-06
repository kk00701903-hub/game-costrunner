using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 언어팩(ko / en). 한글 원문을 키로 쓰는 인라인 방식 — `Loc.T("설정", "Settings")` —
    /// 과, 데이터(챕터 제목·스케줄 이름)용 사전 `Loc.Data(id)`를 함께 제공한다.
    /// 첫 실행은 기기 언어(한국어면 ko, 그 외 en), 설정에서 바꾸면 PlayerPrefs에 남는다.
    public static class Loc
    {
        public const string PrefKey = "CoastRun_Lang";
        private static string _lang;

        public static string Lang
        {
            get
            {
                if (_lang == null)
                {
                    _lang = PlayerPrefs.GetString(PrefKey, "");
                    if (string.IsNullOrEmpty(_lang))
                        _lang = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
                }
                return _lang;
            }
            set
            {
                _lang = value == "en" ? "en" : "ko";
                PlayerPrefs.SetString(PrefKey, _lang);
                PlayerPrefs.Save();
            }
        }

        public static bool IsKo => Lang == "ko";
        public static void Toggle() => Lang = IsKo ? "en" : "ko";

        /// 인라인: 한글 원문 / 영어.
        public static string T(string ko, string en) => IsKo ? ko : en;

        /// 언어 접미사가 붙은 리소스 이름(예: UI_Title_Gate_en). 없으면 기본 이름.
        public static string ResName(string baseName) => baseName + "_" + Lang;

        /// 데이터 사전 — id → 영어. 한국어는 데이터 원문을 그대로 쓴다.
        public static string Data(string id, string ko)
        {
            if (IsKo) return ko;
            return En.TryGetValue(id, out var v) ? v : ko;
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // 챕터 제목
            { "ch.1", "The One Who Came Back" }, { "ch.2", "The Base" }, { "ch.3", "Frequency" }, { "ch.4", "Rua" },
            { "ch.5", "End of Spring" }, { "ch.6", "Festival" }, { "ch.7", "The Sea" }, { "ch.8", "Hospital Day" },
            { "ch.9", "Typhoon" }, { "ch.10", "The Letter" }, { "ch.11", "Tangerines" }, { "ch.12", "Oreum" },
            { "ch.13", "Talk of Seoul" }, { "ch.14", "Words Unsaid" }, { "ch.15", "Fallen Leaves" }, { "ch.16", "First Snow" },
            { "ch.17", "The Tin Can" }, { "ch.18", "91.9" }, { "ch.19", "The Rule" }, { "ch.20", "The Tower" },
            // 계절
            { "season.봄", "Spring" }, { "season.여름", "Summer" }, { "season.가을", "Autumn" }, { "season.겨울", "Winter" },
            // 스케줄 이름
            { "sched.job_orange", "Tangerine Farm" }, { "sched.job_haenyeo", "Help the Haenyeo" }, { "sched.job_cafe", "Beach Cafe" },
            { "sched.job_delivery", "Scooter Delivery" }, { "sched.dev_oreum", "Oreum Walk" }, { "sched.dev_skate", "Skate Practice" },
            { "sched.dev_dance", "Dance Practice" }, { "sched.dev_radio", "Radio Letter" }, { "sched.rest_home", "Laze at Home" },
            { "sched.rest_sea", "Sea Swim" }, { "sched.story", "Go to the Tower" },
            { "sched.job_salon", "Salon Assistant" }, { "sched.job_market", "Market Porter" }, { "sched.job_sashimi", "Night Serving" },
            { "sched.job_night_delivery", "Late-night Delivery" }, { "sched.job_hall", "Village Hall Volunteer" }, { "sched.job_dangsan", "Shrine Preparation" },
            { "sched.job_tower_watch", "Tower Night Patrol" }, { "sched.job_lighthouse", "Lighthouse Cleaning" },
            { "sched.les_skate", "Skate Trick Lessons" }, { "sched.les_gym", "Gym" }, { "sched.les_ham", "Amateur Radio Class" },
            { "sched.les_photo", "Photo & Drawing Class" }, { "sched.les_speech", "Jeju Dialect Class" }, { "sched.les_dance", "Dance Academy" },
            { "sched.les_cook", "Jeju Cooking Class" }, { "sched.les_swim", "Haenyeo School" },
            // 스탯
            { "stat.체력", "Stamina" }, { "stat.순발력", "Agility" }, { "stat.매력", "Charm" }, { "stat.감성", "Sense" },
            { "stat.평판", "Trust" }, { "stat.스트레스", "Stress" }, { "stat.돈", "Money" }, { "stat.하트", "Hearts" },
            // 카테고리
            { "cat.알바", "Jobs" }, { "cat.자기계발", "Growth" }, { "cat.교육", "Lessons" }, { "cat.연습", "Practice" }, { "cat.휴식", "Rest" }, { "cat.스토리", "Story" },
            // 등급/펫
            { "pet.참새", "Sparrow" }, { "pet.오토바이탄 깡패", "Biker" }, { "pet.기러기", "Wild Goose" }, { "pet.없음", "None" },
        };
    }
}
