using System.Text;

namespace CoastRun
{
    /// 213차(사용자: 「단점들 커버해줘」 — 212차 평가: 하루 결산·자동 이동 목록의 이모지가 빈칸/○ 로 보임):
    /// 게임 글꼴(Jua·Pretendard)과 Unity 기본 Text 는 컬러 이모지(U+1F000 이상, 서로게이트 쌍)를 못 그려 빈칸이 된다.
    /// ★ ♥ ● ✕ ▶ 같은 BMP 기호는 글꼴·OS 대체 글꼴로 나오므로 그대로 두고, 그리지 못하는 이모지만 지운다(앞뒤 빈칸도 정리).
    public static class EmojiText
    {
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            bool any = false;
            for (int i = 0; i < s.Length; i++) { char c = s[i]; if (char.IsSurrogate(c) || c == '️' || c == '‍' || c == '⃣') { any = true; break; } }
            if (!any) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsHighSurrogate(c)) { i++; continue; }
                if (char.IsLowSurrogate(c) || c == '️' || c == '‍' || c == '⃣') continue;
                sb.Append(c);
            }
            // 이모지 자리에 남은 겹빈칸·줄 앞 빈칸 정리
            string r = sb.ToString();
            while (r.Contains("  ")) r = r.Replace("  ", " ");
            r = r.Replace("\n ", "\n").Replace("( ", "(").Replace("「 ", "「");
            return r.Trim();
        }
    }
}
