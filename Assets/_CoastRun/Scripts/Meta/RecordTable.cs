using UnityEngine;

namespace CoastRun
{
    /// 37차: 레코드 7장 — 컷씬 음악 M1~M7. 롱컷을 보면 그 곡의 레코드가 열리고(4곡), 컷씬에 안 나오는 3곡은 보너스
    /// (20챕터 중 S급 18개 = 90% 이상). 설정의 비밀코드(1111)는 전부 연다. 타이틀 더보기 › 레코드(RecordsUI)에서 듣는다.
    public static class RecordTable
    {
        public class Track
        {
            public int num;                 // 1..7 → BGM_M<num>
            public string ko, en;           // 곡 제목
            public string noteKo, noteEn;   // 어디서 나오는 곡인지
            public bool bonus;
            public string unlockScene;      // 이 컷씬을 보면 해금(스토리 곡)
            public string unlockKo, unlockEn;
            public Color label;             // 레코드 가운데 라벨 색
            public string Key => "M" + num;
            public string Clip => "BGM_M" + num;
        }

        public const int Chapters = 20;
        public const int BonusNeedS = 18;   // 90% of 20

        public static readonly Track[] All =
        {
            new Track { num = 1, ko = "이별 예감", en = "Premonition of Goodbye", noteKo = "보너스 트랙", noteEn = "Bonus track", bonus = true,
                        unlockKo = "S급 18/20 이상", unlockEn = "Rank S on 18/20", label = new Color(0.55f, 0.62f, 0.85f) },
            new Track { num = 2, ko = "솜사탕 둘이서", en = "Cotton Candy for Two", noteKo = "보너스 트랙", noteEn = "Bonus track", bonus = true,
                        unlockKo = "S급 18/20 이상", unlockEn = "Rank S on 18/20", label = new Color(0.98f, 0.66f, 0.78f) },
            new Track { num = 3, ko = "별 (듀엣)", en = "Star (Duet)", noteKo = "트루 엔딩 · 하늘이 웃는 순간", noteEn = "True ending · when Haneul laughs", unlockScene = "CH15_Open",
                        unlockKo = "롱컷 5 「스무 살」", unlockEn = "Long cut 5 'Twenty'", label = new Color(0.40f, 0.36f, 0.62f) },
            new Track { num = 4, ko = "Goodbye My First Love", en = "Goodbye My First Love", noteKo = "롱컷 3 열흘의 냉전 · 롱컷 5 차 안 라디오", noteEn = "Long cut 3 · car radio in long cut 5", unlockScene = "CH10_Open",
                        unlockKo = "롱컷 3 「열두 개의 초」", unlockEn = "Long cut 3 'Twelve Candles'", label = new Color(0.86f, 0.50f, 0.42f) },
            new Track { num = 5, ko = "돌아온 제주", en = "Back to Jeju", noteKo = "오프닝 타이틀 · 엔딩 후렴", noteEn = "Opening title · ending chorus", unlockScene = "CH07_Open",
                        unlockKo = "롱컷 2 「우리 기지」", unlockEn = "Long cut 2 'Our Base'", label = new Color(0.30f, 0.68f, 0.72f) },
            new Track { num = 6, ko = "남녀 사랑 이야기 (inst.)", en = "A Love Story (inst.)", noteKo = "아빠의 곡 · 롱컷 전편", noteEn = "Dad's song · every long cut", unlockScene = "CH04_Open",
                        unlockKo = "롱컷 1 「하트」", unlockEn = "Long cut 1 'Heart'", label = new Color(0.82f, 0.64f, 0.30f) },
            new Track { num = 7, ko = "보조개", en = "Dimple", noteKo = "보너스 트랙", noteEn = "Bonus track", bonus = true,
                        unlockKo = "S급 18/20 이상", unlockEn = "Rank S on 18/20", label = new Color(0.98f, 0.80f, 0.45f) },
        };

        private static MetaProfile P => GameManager.I != null ? GameManager.I.Profile : null;

        public static int SCount(MetaProfile p)
        {
            if (p == null || p.trackGrade == null) return 0;
            int n = 0;
            for (int i = 0; i < p.trackGrade.Length && i < Chapters; i++) if (p.trackGrade[i] >= 4) n++;
            return n;
        }

        public static bool BonusOpen(MetaProfile p) => p != null && (p.devUnlockAll || SCount(p) >= BonusNeedS);

        public static bool IsUnlocked(MetaProfile p, Track t)
        {
            if (p == null || t == null) return false;
            if (p.devUnlockAll) return true;
            if (t.bonus) return BonusOpen(p);
            return (p.recordMask & (1 << (t.num - 1))) != 0;
        }

        public static bool IsNew(MetaProfile p, Track t) => p != null && (p.recordNewMask & (1 << (t.num - 1))) != 0;

        public static void MarkSeen(MetaProfile p, Track t)
        {
            if (p == null) return;
            p.recordNewMask &= ~(1 << (t.num - 1));
        }

        public static int UnlockedCount(MetaProfile p)
        {
            int n = 0;
            foreach (var t in All) if (IsUnlocked(p, t)) n++;
            return n;
        }

        public static bool HasNew(MetaProfile p)
        {
            if (p == null) return false;
            foreach (var t in All) if (IsUnlocked(p, t) && IsNew(p, t)) return true;
            return false;
        }

        /// ChapterVN.Finish 에서 호출 — 롱컷 씬이면 그 곡의 레코드를 연다.
        public static void OnSceneWatched(string sceneId)
        {
            var p = P; if (p == null || string.IsNullOrEmpty(sceneId)) return;
            bool changed = false;
            foreach (var t in All)
            {
                if (t.bonus || t.unlockScene != sceneId) continue;
                int bit = 1 << (t.num - 1);
                if ((p.recordMask & bit) != 0) continue;
                p.recordMask |= bit; p.recordNewMask |= bit; changed = true;
                CoastToast.Show(Loc.T($"레코드 해금 — {t.ko}", $"Record unlocked — {t.en}"));
            }
            if (changed) GameManager.I.WriteProfileNow();
        }

        /// 비밀코드 — 전부 연다(챕터는 RaisingUI 타임라인이 devUnlockAll 을 본다).
        public static void UnlockAll(MetaProfile p)
        {
            if (p == null) return;
            p.devUnlockAll = true;
            p.recordMask = (1 << All.Length) - 1;
            p.recordNewMask = p.recordMask;
        }
    }
}
