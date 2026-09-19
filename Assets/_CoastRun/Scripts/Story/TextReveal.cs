using System.Text;
using UnityEngine;

namespace CoastRun.Story
{
    /// 136차: 컷씬·VN 자막을 「읽어 주듯」 한 글자씩 드러내는 리치텍스트 빌더.
    /// 아직 안 나온 글자는 알파 0 으로 같이 넣어 줄바꿈이 흔들리지 않고, 방금 나온 글자 2~3개는 따뜻한 색으로 빛난다.
    public static class TextReveal
    {
        const string Hidden = "<color=#00000000>";
        const string Hi1 = "<color=#FFF0B8FF>";   // 막 나온 글자
        const string Hi2 = "<color=#FFF8E0FF>";   // 그 앞 글자
        const string End = "</color>";
        static readonly StringBuilder Sb = new StringBuilder(512);

        /// shown = 드러난 글자 수(실수). 전부 드러나면 원문 그대로 돌려준다.
        public static string Build(string full, float shown, bool glow = true)
        {
            if (string.IsNullOrEmpty(full)) return "";
            int n = Mathf.Clamp(Mathf.FloorToInt(shown), 0, full.Length);
            if (n >= full.Length) return full;
            Sb.Length = 0;
            int hi = glow ? Mathf.Min(3, n) : 0;
            int plain = n - hi;
            if (plain > 0) Sb.Append(full, 0, plain);
            if (hi > 0)
            {
                // 뒤에서부터 1글자(가장 밝게), 나머지(살짝 밝게)
                if (hi > 1) { Sb.Append(Hi2).Append(full, plain, hi - 1).Append(End); }
                Sb.Append(Hi1).Append(full, n - 1, 1).Append(End);
            }
            Sb.Append(Hidden).Append(full, n, full.Length - n).Append(End);
            return Sb.ToString();
        }

        /// 글자 수·컷 길이로 초당 글자 수를 정한다: 컷의 앞 60% 안에 다 읽히되 너무 빠르지 않게(14~40cps).
        public static float Cps(string text, float seconds)
        {
            int len = text == null ? 0 : text.Length;
            float window = Mathf.Max(0.8f, seconds * 0.6f);
            return Mathf.Clamp(len / window, 14f, 40f);
        }
    }
}
