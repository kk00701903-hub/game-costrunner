using System;
using UnityEngine;

namespace CoastRun
{
    public enum ChapterGrade { None = 0, C = 1, B = 2, A = 3, S = 4 }
    public enum EndingKind { None = 0, Happy = 1, Tragic = 2 }
    /// 이동 모드 — 회차 시작 시 캐릭터 선택으로 고정된다.
    public enum RunMode { Running = 0, Skateboard = 1 }

    /// 육성 스탯 4종 + 재화. 값은 모두 정수, Clamp()로 범위를 지킨다.
    [Serializable]
    public class PlayerStats
    {
        public const int StatMax = 200;

        public int stamina = 30;   // 체력
        public int agility = 20;   // 순발력
        public int charm = 20;     // 매력
        public int stress = 0;     // 스트레스
        public int money = 300;    // 돈
        public int hearts = 0;     // 말랑이 하트 누적(회차 전체)

        public PlayerStats Clone() => (PlayerStats)MemberwiseClone();

        public int Get(StatKind kind)
        {
            switch (kind)
            {
                case StatKind.Stamina: return stamina;
                case StatKind.Agility: return agility;
                case StatKind.Charm: return charm;
                case StatKind.Stress: return stress;
                default: return 0;
            }
        }

        public void Clamp()
        {
            stamina = Mathf.Clamp(stamina, 0, StatMax);
            agility = Mathf.Clamp(agility, 0, StatMax);
            charm = Mathf.Clamp(charm, 0, StatMax);
            stress = Mathf.Clamp(stress, 0, StatMax);
            money = Mathf.Max(0, money);
            hearts = Mathf.Max(0, hearts);
        }

        /// 스트레스가 체력을 넘으면 번아웃: 실패율 급증, 대성공 거의 없음.
        public bool Burnout => stress > stamina;
    }

    public enum StatKind { None = 0, Stamina = 1, Agility = 2, Charm = 3, Stress = 4 }

    /// 챕터 1개의 영구 기록. 타임라인 재도전은 이 객체만 덮어쓴다.
    [Serializable]
    public class ChapterRecord
    {
        public int chapter;             // 1..20
        public int weekStart;
        public int weekEnd;
        public int heartsEarned;        // 이 챕터에서 얻은 말랑이 하트
        public int heartsTarget;        // 만점. earned/target >= 0.9 → S
        public ChapterGrade grade;
        public bool cleared;            // 런닝 클리어 + 컷씬까지 본 챕터
        public PlayerStats snapshotAtStart;

        public float Ratio => heartsTarget > 0 ? (float)heartsEarned / heartsTarget : 0f;
    }

    /// 회차 진행 상태 전체. save_0.json 한 파일에 JsonUtility로 직렬화.
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public int week = 1;                 // 1..52
        public int chapter = 1;              // 1..20
        public int phaseIndex = 0;           // 이번 주에 소화한 페이즈 수 0..3
        public PlayerStats stats = new PlayerStats();
        public ChapterRecord[] chapters = new ChapterRecord[Timeline.Chapters];
        public int chapterHearts;            // 진행 중 챕터에서 지금까지 모은 하트
        public PetKind equippedPet = PetKind.None;
        public int ownedPetMask;
        public string[] queuedSchedule = new string[3];
        public EndingKind reachedEnding = EndingKind.None;
        public int playthrough = 1;
        public RunMode runMode = RunMode.Running;
        public bool prologueSeen;
        public int seed;
        public int rollCount;

        public ChapterRecord CurrentChapter =>
            chapters != null && chapter >= 1 && chapter <= chapters.Length ? chapters[chapter - 1] : null;

        public bool HasQueuedSchedule
        {
            get
            {
                if (queuedSchedule == null) return false;
                for (int i = 0; i < queuedSchedule.Length; i++)
                    if (!string.IsNullOrEmpty(queuedSchedule[i])) return true;
                return false;
            }
        }
    }

    /// 세이브 슬롯과 무관한 계정 프로필: 회차 간 해금. profile.json
    [Serializable]
    public class MetaProfile
    {
        public int endingsSeen;
        public int happyEndings;
        public bool skateboardUnlocked;
        public int bestPlaythrough;
    }
}
