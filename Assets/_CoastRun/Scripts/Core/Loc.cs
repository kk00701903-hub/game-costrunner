using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CoastRun
{
    /// 언어팩(ko / en). 한글 원문을 키로 쓰는 인라인 방식 — `Loc.T("설정", "Settings")` —
    /// 과, 데이터(챕터 제목·스케줄 이름)용 사전 `Loc.Data(id)`를 함께 제공한다.
    /// 첫 실행은 기기/스토어 언어(미지원이면 en — 219차), 설정에서 한 번 고르면 PlayerPrefs에 남는다.
    public static class Loc
    {
        public const string PrefKey = "CoastRun_Lang";
        /// 지원 언어 순서(설정 토글 순서). ko/en 은 코드 안 인라인, 나머지는 Resources/CoastRun/Lang/lang_<code>.txt (영어 → 번역 표).
        /// 219차(사용자 결정): 메뉴에 보이는 언어는 ko·en·ja·es 4개. th·id 번역표 파일은 보존만(설정에 안 보임, 저장값이 th/id 면 en).
        public static readonly string[] Langs = { "ko", "en", "ja", "es" };
        private static string _lang;
        private static Dictionary<string, string> _table;
        private static string _tableLang;

        public static string Lang
        {
            get
            {
                if (_lang == null)
                {
                    _lang = PlayerPrefs.GetString(PrefKey, "");
                    if (string.IsNullOrEmpty(_lang)) _lang = FromSystem();
                    else if (System.Array.IndexOf(Langs, _lang) < 0) _lang = "en";   // 219차: 예전에 고른 th/id → 영어
                }
                return _lang;
            }
            set
            {
                _lang = System.Array.IndexOf(Langs, value) >= 0 ? value : "en";
                PlayerPrefs.SetString(PrefKey, _lang);
                PlayerPrefs.Save();
            }
        }

        static string FromSystem()
        {
            // 스토어/기기 언어. 219차: 지원 목록(ko·en·ja·es)에 없으면 영어(전 세계 출시). 한국어 기기는 그대로 한국어.
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Spanish: return "es";
                default: return "en";
            }
        }

        public static bool IsKo => Lang == "ko";
        public static bool IsEn => Lang == "en";

        /// 설정 토글: ko → en → ja → es → ko
        public static void Toggle()
        {
            int i = System.Array.IndexOf(Langs, Lang);
            Lang = Langs[(i + 1) % Langs.Length];
        }

        public static string Native(string code)
        {
            switch (code)
            {
                case "ko": return "한국어";
                case "ja": return "日本語";
                case "id": return "Bahasa Indonesia";
                case "th": return "ไทย";
                case "es": return "Español";
                default: return "English";
            }
        }
        /// 설정에서 고른 언어를 저장하고 적용. (한 번 고르면 PrefKey에 남음)
        public static void SetLang(string code)
        {
            Lang = code;
            _table = null;
            _tableLang = null;
        }

        public static string LanguageButtonLabel() =>
            Loc.T($"언어: {Native(Lang)}", Tr("Language") + $": {Native(Lang)}");

        /// 인라인: 한글 원문 / 영어 (그 외 언어는 영어를 키로 번역표 조회, 없으면 영어).
        public static string T(string ko, string en) => IsKo ? ko : Tr(en);

        /// 영어 문장 → 현재 언어. 표에 없으면 영어 그대로.
        public static string Tr(string en)
        {
            if (IsKo || IsEn || string.IsNullOrEmpty(en)) return en;
            var t = Table();
            if (t != null && t.TryGetValue(en, out var v) && !string.IsNullOrEmpty(v)) return v;
            return FromTemplate(en) ?? en;
        }

        // ── 219차: 조합 문장(영어 쪽이 $"…{값}…") 번역 — 표의 키에 {0} {1} … 가 있으면 틀로 쓴다.
        //   런타임 영어 문장을 틀에 맞춰 값을 뽑고, 번역문의 {n} 자리에 넣는다(값이 표에 있는 단어면 그것도 번역).
        private sealed class Fmt { public Regex Re; public int[] Idx; public string Val; public int Lit; }
        private static List<Fmt> _fmt;
        private static Dictionary<string, string> _fmtCache;

        static void AddFmt(string key, string val)
        {
            var parts = Regex.Split(key, @"\{(\d+)\}");
            if (parts.Length < 3) return;
            var sb = new StringBuilder("^"); var idx = new List<int>(); int lit = 0, len = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0) { sb.Append(Regex.Escape(parts[i])); foreach (char c in parts[i]) if (char.IsLetter(c)) lit++; len += parts[i].Length; }
                else { sb.Append("(.+?)"); idx.Add(int.Parse(parts[i])); }
            }
            if (lit < 2) return;   // 「{0}」 「{0} {1}」 처럼 글자가 거의 없는 틀은 아무 문장에나 맞으므로 쓰지 않는다
            sb.Append('$');
            _fmt.Add(new Fmt { Re = new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant), Idx = idx.ToArray(), Val = val, Lit = len });   // Lit = 틀의 고정 글자 수(정렬용)
        }

        static string FromTemplate(string en)
        {
            if (_fmt == null || _fmt.Count == 0) return null;
            if (_fmtCache.TryGetValue(en, out var hit)) return hit;
            string res = null;
            foreach (var f in _fmt)
            {
                var m = f.Re.Match(en);
                if (!m.Success) continue;
                string outS = f.Val;
                for (int g = 0; g < f.Idx.Length; g++)
                {
                    string cap = m.Groups[g + 1].Value;
                    if (_table != null && _table.TryGetValue(cap, out var cv) && !string.IsNullOrEmpty(cv)) cap = cv;   // 값이 번역표 단어면 그것도
                    outS = outS.Replace("{" + f.Idx[g] + "}", cap);
                }
                res = outS; break;
            }
            if (_fmtCache.Count > 1024) _fmtCache.Clear();   // 시간·점수처럼 계속 바뀌는 문장이 쌓이지 않게
            _fmtCache[en] = res;
            return res;
        }

        static Dictionary<string, string> Table()
        {
            if (_table != null && _tableLang == Lang) return _table;
            _tableLang = Lang;
            _table = new Dictionary<string, string>();
            _fmt = new List<Fmt>(); _fmtCache = new Dictionary<string, string>();
            var ta = Resources.Load<TextAsset>("CoastRun/Lang/lang_" + Lang);
            if (ta == null) return _table;
            foreach (var raw in ta.text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string k = line.Substring(0, tab).Replace("\\n", "\n");
                string v = line.Substring(tab + 1).Replace("\\n", "\n");
                _table[k] = v;
                if (k.IndexOf('{') >= 0 && Regex.IsMatch(k, @"\{\d+\}")) AddFmt(k, v);
            }
            _fmt.Sort((a, b) => b.Lit.CompareTo(a.Lit));   // 글자가 많은(구체적인) 틀부터
            return _table;
        }

        /// 언어 접미사가 붙은 리소스 이름(예: UI_Title_Gate_en). ko 외에는 en 그림을 쓴다.
        public static string ResName(string baseName) => baseName + "_" + (IsKo ? "ko" : "en");

        /// 데이터 사전 — id → 영어(→ 번역표). 한국어는 데이터 원문을 그대로 쓴다.
        public static string Data(string id, string ko)
        {
            if (IsKo) return ko;
            return En.TryGetValue(id, out var v) ? Tr(v) : ko;
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // 챕터 제목
            // 219차: 16·18장 외 한국어로 남아 있던 영어 제목 정리
            { "ch.1", "A Face I Don't Know" }, { "ch.2", "The Board" }, { "ch.3", "The Face in the Photo" }, { "ch.4", "Splashing" },
            { "ch.5", "A Meal" }, { "ch.6", "Taewak" }, { "ch.7", "The House on the Hill" }, { "ch.8", "Two Bottles of Milk" },
            { "ch.9", "First Snow" }, { "ch.10", "The Blue Headband" }, { "ch.11", "The White Shirt" }, { "ch.12", "Our Hideout" },
            { "ch.13", "Typhoon" }, { "ch.14", "The Handcart" }, { "ch.15", "Twentieth Birthday" }, { "ch.16", "Passing by" },
            { "ch.17", "Now I Know Everything" }, { "ch.18", "Quiet" }, { "ch.19", "Too Far to Hold" }, { "ch.20", "Under the Tower" },
            // 계절
            { "season.봄", "Spring" }, { "season.여름", "Summer" }, { "season.가을", "Autumn" }, { "season.겨울", "Winter" },
            // 스케줄 이름
            { "sched.job_orange", "Tangerine Farm" }, { "sched.job_haenyeo", "Help the Haenyeo" }, { "sched.job_cafe", "Beach Cafe" },
            { "sched.job_delivery", "Scooter Delivery" }, { "sched.dev_oreum", "Oreum Walk" }, { "sched.dev_skate", "Skate Practice" },
            { "sched.dev_dance", "Dance Practice" }, { "sched.dev_radio", "Radio Letter" }, { "sched.rest_home", "Eat & Rest" },
            { "sched.rest_nap", "Nap" }, { "sched.rest_sea", "Sea Swim" }, { "sched.story", "Go to the Tower" },
            { "sched.job_salon", "Salon Assistant" }, { "sched.job_market", "Market Porter" }, { "sched.job_sashimi", "Night Serving" },
            { "sched.job_night_delivery", "Late-night Delivery" }, { "sched.job_hall", "Village Hall Volunteer" }, { "sched.job_dangsan", "Shrine Preparation" },
            { "sched.job_tower_watch", "Tower Night Patrol" }, { "sched.job_lighthouse", "Lighthouse Cleaning" }, { "sched.job_tower_fix", "Tower Maintenance Helper" }, { "sched.job_dj_assist", "Radio Station Assistant" },
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
