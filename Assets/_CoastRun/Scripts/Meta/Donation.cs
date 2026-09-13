using System;
using UnityEngine;

namespace CoastRun
{
    /// 52차(사용자): 「기부부탁」 — 광고·현질 없는 게임을 지속 업데이트하기 위한 커피 한 잔 값($4, 구글 결제 소모성 상품) 기부. 63차(사용자): $3 → $4.
    ///   기부자는 선물 하나를 고른다: ① 히든 트랙(M9·M10 — 레코드에 열리고 K-POP 런 풀에도 들어감) ② 모든 게임 열림(히든 패스코드 — 다른 기기에서도 설정 › 비밀코드로)
    ///   ③ 아무것도 안 받음 ④ 74차: OST 잠금해제(레코드 M2~M7 — 기부로만 열린다). 여러 번 기부 가능(잔 수 누적). 상태는 MetaProfile(profile.json).
    public static class Donation
    {
        public enum Gift { None = 0, HiddenTrack = 1, UnlockAll = 2, Ost = 4 }   // 74차(사용자): ④ OST 잠금해제(레코드 M2~M7)

        public const string ProductId = "coastrun_donate_coffee";   // 소모성, ₩5,500 / $3.99 (63차: $4)
        public const string PriceLabelFallback = "$4";
        /// 기부 선물 ② 패스코드 — 설정 › 비밀코드에 넣으면 다른 기기에서도 전부 열린다(테스트용 1111 과 별개).
        public const string DonorPasscode = "7799";
        public const string SeenKey = "CoastRun_DonateSeen";

        private static MetaProfile P => GameManager.I != null ? GameManager.I.Profile : null;

        public static int Cups => P != null ? P.donateCups : 0;
        public static bool HiddenTrack => P != null && (P.donateGiftMask & (int)Gift.HiddenTrack) != 0;
        public static bool AllOpen => P != null && (P.donateGiftMask & (int)Gift.UnlockAll) != 0;
        /// 74차: 레코드 OST 잠금해제(기부 선물 ④). M1 은 기본 공개, 나머지는 이것으로만 열린다.
        public static bool OstOpen => P != null && (P.donateGiftMask & (int)Gift.Ost) != 0;
        public static string PriceLabel
        {
            get
            {
#if UNITY_EDITOR
                return PriceLabelFallback;   // 에디터 가짜 스토어는 $0.01 을 돌려준다
#else
                return !string.IsNullOrEmpty(IapBridge.DonatePrice) ? IapBridge.DonatePrice : PriceLabelFallback;
#endif
            }
        }

        /// 첫 로딩에 팝업을 자동으로 띄웠나(이후엔 우상단 아이콘만).
        public static bool PopupSeen => PlayerPrefs.GetInt(SeenKey, 0) == 1;
        public static void MarkPopupSeen() { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); }

        /// 구글 결제 → 성공하면 선물 적용.
        public static void Donate(Gift gift, Action<bool> done)
        {
            IapBridge.Purchase(ProductId, ok =>
            {
                if (ok) Grant(gift);
                done?.Invoke(ok);
            });
        }

        /// 결제 확정(또는 복원)마다 한 잔 추가 + 선물 적용.
        public static void Grant(Gift gift)
        {
            var p = P; if (p == null) return;
            p.donateCups++;
            p.donateGiftMask |= (int)gift;
            if (gift == Gift.HiddenTrack) { p.recordNewMask |= RecordTable.HiddenBits; }
            if (gift == Gift.Ost) { p.recordNewMask |= (1 << RecordTable.All.Length) - 2; }   // M2~ 전부 NEW
            GameManager.I.WriteProfileNow();
            CoastToast.Show(Loc.T($"☕ 고마워요! 기부 {p.donateCups}잔", $"☕ Thank you! {p.donateCups} cup(s)"));
        }

        /// 설정 › 비밀코드에 패스코드를 넣었을 때 — 기부 선물 ②를 이 기기에 적용.
        public static bool TryPasscode(string code)
        {
            if (code != DonorPasscode) return false;
            var p = P; if (p == null) return false;
            p.donateGiftMask |= (int)Gift.UnlockAll;
            GameManager.I.WriteProfileNow();
            return true;
        }
    }
}
