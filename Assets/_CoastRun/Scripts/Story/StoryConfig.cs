using UnityEngine;
using UnityEngine.Video;

namespace CoastRun
{
    public enum DayPhase
    {
        BrightNoon = 0,
        GoldenHour = 1,
        BlueHour = 2
    }

    [System.Serializable]
    public struct PrologueBeat
    {
        public string title;
        [TextArea(2, 6)] public string body;
        public float holdSeconds;
        [Tooltip("Resources/CoastRun/Scene/ name without path. Empty = auto from beat index.")]
        public string sceneImage;
    }

    [System.Serializable]
    public struct LandmarkBeat
    {
        [Tooltip("Normalized 0..1 along path to tower, or absolute metres if useAbsoluteMetres.")]
        public float trigger;
        public bool useAbsoluteMetres;
        public string title;
        [TextArea(2, 5)] public string monologue;
    }

    [System.Serializable]
    public struct ArrivalBeat
    {
        public string title;
        [TextArea(2, 6)] public string body;
        public float holdSeconds;
    }

    /// Story + destination narrative for 『우리의 송전탑』 — dense landmarks for long ride.
    [CreateAssetMenu(menuName = "Coast Run/Story Config", fileName = "StoryConfig")]
    public class StoryConfig : ScriptableObject
    {
        [Header("Prologue")]
        public bool playPrologue = true;
        public VideoClip prologueVideo;
        public PrologueBeat[] prologueBeats =
        {
            new PrologueBeat
            {
                title = "약속의 스마트폰",
                body = "「노을 질 때, 우리 어릴 적 비밀 기지였던 그 송전탑 아래에서 만나자.\n꼭 할 말이 있어.」\n\n멀리 아스라히 보이는 송전탑 사진이 함께 와 있다.",
                holdSeconds = 5.5f
            },
            new PrologueBeat
            {
                title = "예기치 못한 장애",
                body = "정류장 전광판 — 『정비 중 · 운행 중단』.\n택시를 잡으려 해도 축제로 도로가 막혔고,\n지나가는 차들은 모두 예약등만 켠 채 스쳐 간다.",
                holdSeconds = 5f
            },
            new PrologueBeat
            {
                title = "소녀의 결심",
                body = "해가 기울기 시작한다.\n배낭에 묶인 보드를 풀고, 땅에 내려놓는다.\n수평선 너머 송전탑을 바라보며 — 힘차게 땅을 걷어찬다.",
                holdSeconds = 4.5f
            },
            new PrologueBeat
            {
                title = "게임플레이로의 전환",
                body = "카메라가 등 뒤로 물러난다.\n해안가 내리막, 송전탑을 향한 질주가 시작된다.",
                holdSeconds = 2.5f
            }
        };

        [Header("Story act progression (Prologue → Run → Golden → Blue → Arrival)")]
        public StoryActBeat[] actBeats =
        {
            new StoryActBeat
            {
                act = StoryAct.Run,
                normalizedProgress = 0f,
                hudLabel = "CHASE",
                narratorLine = "바람을 가르며… 추억이 길을 안내한다."
            },
            new StoryActBeat
            {
                act = StoryAct.GoldenHour,
                normalizedProgress = 0.55f,
                hudLabel = "GOLDEN HOUR",
                narratorLine = "하늘이 주황으로 물든다. 노을 전에 꼭 도착해야 해."
            },
            new StoryActBeat
            {
                act = StoryAct.BlueHourApproach,
                normalizedProgress = 0.88f,
                hudLabel = "BLUE HOUR",
                narratorLine = "보랏빛 송전탑… 조금만 더. 기다려줘."
            },
            new StoryActBeat
            {
                act = StoryAct.Arrival,
                normalizedProgress = 1f,
                hudLabel = "",
                narratorLine = ""
            }
        };

        [Header("Arrival — unused; ending lives in 04_Ending (no reunion copy)")]
        public ArrivalBeat[] arrivalBeats =
        {
            new ArrivalBeat
            {
                title = "",
                body = "",
                holdSeconds = 0f
            }
        };

        [Header("Landmarks (옛 추억) — 장거리용")]
        public LandmarkBeat[] landmarks =
        {
            L(0.04f, "출발 · 정류장", "버스가 안 오면, 보드가 답이지."),
            L(0.08f, "봄 · 벚꽃 가로수", "분홍 비 같던 그 봄… 같이 뛰었지."),
            L(0.12f, "추억 · 방파제", "아이스크림 녹이던 방파제. 바람이 같아."),
            L(0.16f, "카페 골목", "창가 자리… 네가 먼저 고르던 곳."),
            L(0.20f, "전신주 골목", "『비밀 기지까지 누가 먼저』 내기."),
            L(0.25f, "여름 · 해수욕장", "파도 소리에 말을 삼키던 여름."),
            L(0.30f, "서프숍 앞", "빌려 탄 보드, 무릎 까짐, 웃음."),
            L(0.35f, "축제 행렬", "오늘도 길이 막혔어. 그때도."),
            L(0.40f, "자판기 코너", "따뜻한 캔커피 나눠 마시던 밤."),
            L(0.45f, "가을 · 낙엽", "낙엽 밟는 소리… 네가 좋아했지."),
            L(0.50f, "언덕 전망", "여기서 송전탑이 처음으로 보여."),
            L(0.55f, "비 오던 날", "우산 하나. 어깨가 젖었어."),
            L(0.60f, "등대 아래", "등대 불빛에 약속했지. 꼭 돌아오자고."),
            L(0.65f, "겨울 · 첫눈", "첫눈에 손 잡던 날. 손가락이 시렸어."),
            L(0.70f, "눈 쌓인 산책로", "보드 바퀴가 하얘지던 겨울."),
            L(0.75f, "철길 건너", "기적 소리에 말을 멈췄던 곳."),
            L(0.80f, "노을 직전", "하늘이 주황으로 물들기 시작해."),
            L(0.85f, "블루아워", "보랏빛… 거의 다 왔어."),
            L(0.90f, "송전탑 실루엣", "저기다. 어릴 적 비밀 기지."),
            L(0.95f, "마지막 굽이", "조금만 더… 기다려줘.")
        };

        [Header("Near-miss story lines")]
        public string[] nearMissCheerLines =
        {
            "조금만 더 기다려줘!",
            "아직이야… 늦지 않을 거야.",
            "송전탑까지, 한 번에!",
            "노을 전에 꼭 갈게.",
            "기다려… 금방이야!",
            "비 와도 괜찮아.",
            "눈이라도… 갈게.",
            "봄·여름·가을·겨울, 다 달려서!",
            "바퀴야, 조금만 더!",
            "그 사람… 아직 있지?"
        };

        [Header("Destination UI")]
        public float dDaySeconds = ContentPace.DDaySeconds;
        public string boyfriendLabel = "그 사람";
        public string towerLabel = "송전탑";

        [Header("Day cycle (normalized progress 0→1 to tower)")]
        public float goldenHourAt = 0.55f;
        public float blueHourAt = 0.88f;

        private static LandmarkBeat L(float t, string title, string line) => new LandmarkBeat
        {
            trigger = t,
            title = title,
            monologue = line
        };
    }
}
