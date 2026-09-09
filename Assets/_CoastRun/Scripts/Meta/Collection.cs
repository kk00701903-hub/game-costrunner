using UnityEngine;

namespace CoastRun
{
    /// 컬렉션(레코드 20·포토카드 30) 해금 규칙과 유료 권한. 상태는 MetaProfile(profile.json)에 남는다.
    public static class Collection
    {
        private static MetaProfile P => GameManager.I != null ? GameManager.I.Profile : null;
        private static void Save() { if (GameManager.I != null) GameManager.I.WriteProfileNow(); }

        // ── 유료 권한 ──
        public const int FreeChapters = 5;              // 봄 시즌 무료
        public static bool AlbumOwned => (P != null && P.albumOwned) || PlayerPrefs.GetInt("CoastRun_AlbumOwned", 0) == 1;
        public static bool CanPlayChapter(int chapter) => true;   // 38차: 디지털 앨범 구매 삭제 — 전 챕터 무료
        public static void GrantAlbum()
        {
            PlayerPrefs.SetInt("CoastRun_AlbumOwned", 1); PlayerPrefs.Save();
            if (P != null) { P.albumOwned = true; Save(); }
        }
        public static string PriceLabel => IapBridge.LocalizedPrice ?? Loc.T("₩9,900", "$8.99");

        // ── 트랙 ──
        public static ChapterGrade TrackGrade(int chapter)
        {
            var p = P; if (p == null || p.trackGrade == null || p.trackGrade.Length < 20) return ChapterGrade.None;
            return (ChapterGrade)p.trackGrade[Mathf.Clamp(chapter, 1, 20) - 1];
        }
        /// 22차-8: chapter = 트랙의 해금 챕터.
        public static bool TrackUnlocked(int chapter)
        {
            var t = AlbumTable.ForChapter(chapter);
            if (t == null) return false;
            return AlbumTable.UnlockedBy(t.Value, TrackGrade(chapter));
        }
        public static int TracksUnlocked { get { int n = 0; foreach (var t in AlbumTable.Tracks) if (TrackUnlocked(t.chapter)) n++; return n; } }

        // ── 카드 ──
        public static bool HasCard(int id) => P != null && (P.cardMask & (1 << (id - 1))) != 0;
        public static bool CardSigned(int id) => P != null && (P.cardSignedMask & (1 << (id - 1))) != 0;
        public static bool CardIsNew(int id) => P != null && (P.cardNewMask & (1 << (id - 1))) != 0;
        public static int CardsOwned { get { int n = 0; for (int i = 1; i <= PhotocardTable.Count; i++) if (HasCard(i)) n++; return n; } }
        public static int NewCards { get { int n = 0; for (int i = 1; i <= PhotocardTable.Count; i++) if (CardIsNew(i)) n++; return n; } }

        public static void ClearNew(int id)
        {
            var p = P; if (p == null) return;
            p.cardNewMask &= ~(1 << (id - 1)); Save();
        }

        /// 반환: 새로 얻었으면 true.
        public static bool GiveCard(int id, bool signed = false)
        {
            var p = P; if (p == null) return false;
            int bit = 1 << (id - 1);
            bool fresh = (p.cardMask & bit) == 0;
            bool freshSign = signed && (p.cardSignedMask & bit) == 0;
            p.cardMask |= bit;
            if (signed) p.cardSignedMask |= bit;
            if (fresh || freshSign) p.cardNewMask |= bit;
            Save();
            return fresh || freshSign;
        }

        // ── 38차: 포토카드 등급·드롭 ──
        /// 등급별 드롭 확률(%). 러닝 중 포토카드 아이템을 먹으면 등급을 뽑고 그 등급의 안 가진 카드 하나를 준다.
        public static readonly int[] GradeWeights = { 55, 30, 12, 3 };   // N / R / SR / SSR
        public static string GradeName(CardGrade g) => g == CardGrade.SSR ? "SSR" : g == CardGrade.SR ? "SR" : g == CardGrade.R ? "R" : "N";
        public static Color GradeColor(CardGrade g) => g == CardGrade.SSR ? new Color(1f, 0.62f, 0.18f) : g == CardGrade.SR ? new Color(0.72f, 0.42f, 0.95f) : g == CardGrade.R ? new Color(0.30f, 0.62f, 0.98f) : new Color(0.55f, 0.72f, 0.60f);

        /// 러닝 아이템 드롭. 반환: 얻은 카드 id(0 = 전부 보유 → 코인 보상).
        public static int RollCardDrop(System.Random rng)
        {
            var p = P; if (p == null) return 0;
            int r = rng.Next(100), acc = 0; int gi = 0;
            for (int i = 0; i < GradeWeights.Length; i++) { acc += GradeWeights[i]; if (r < acc) { gi = i; break; } }
            // 뽑힌 등급에 남은 카드가 없으면 아래 등급 → 위 등급 순으로
            int[] order = { gi, gi - 1, gi - 2, gi - 3, gi + 1, gi + 2, gi + 3 };
            foreach (int g in order)
            {
                if (g < 0 || g > 3) continue;
                var pool = new System.Collections.Generic.List<int>();
                for (int id = 1; id <= PhotocardTable.Count; id++)
                    if (!HasCard(id) && PhotocardTable.GradeOf(id) == (CardGrade)g) pool.Add(id);
                if (pool.Count == 0) continue;
                int pick = pool[rng.Next(pool.Count)];
                GiveCard(pick);
                return pick;
            }
            return 0;
        }

        // ── 훅 ──
        /// 런 정산 직후(GameManager.OnRunCleared). 트랙 등급 갱신 + 챕터 카드 + 만남 카드.
        public static void OnChapterSettled(int chapter, ChapterGrade grade, bool wasRetry)
        {
            var p = P; if (p == null) return;
            if (p.trackGrade == null || p.trackGrade.Length < 20) p.trackGrade = new int[20];
            int i = Mathf.Clamp(chapter, 1, 20) - 1;
            bool trackImproved = (int)grade > p.trackGrade[i];
            if (trackImproved) p.trackGrade[i] = (int)grade;
            GiveCard(chapter, signed: grade == ChapterGrade.S);
            if (grade == ChapterGrade.S)
            {
                if (chapter == 5) GiveCard(21, true);
                if (chapter == 10) GiveCard(22, true);
                if (chapter == 15) GiveCard(23, true);
                if (chapter == 20) GiveCard(24, true);
                if (wasRetry) GiveCard(28);
            }
            Save();
        }

        public static void OnEnding(EndingKind kind)
        {
            if (kind == EndingKind.Happy) GiveCard(29, true);
            if (kind == EndingKind.Tragic) GiveCard(30);
        }

        public static void OnRadioGreat()
        {
            var p = P; if (p == null) return;
            p.radioGreatCount++;
            if (p.radioGreatCount >= 3) GiveCard(25);
            Save();
        }

        public static void CheckStatCards(PlayerStats s)
        {
            if (s == null) return;
            if (s.trust >= 50) GiveCard(26);
            if (s.sense >= 60) GiveCard(27);
        }

        /// 개발/테스트: 전부 해금.
        public static void DebugUnlockAll()
        {
            var p = P; if (p == null) return;
            for (int i = 0; i < 20; i++) p.trackGrade[i] = 4;
            p.cardMask = (1 << 30) - 1; p.cardSignedMask = (1 << 20) - 1; p.cardNewMask = p.cardMask;
            Save();
        }
    }
}
