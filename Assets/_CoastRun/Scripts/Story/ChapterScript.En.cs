using System.Collections.Generic;

namespace CoastRun
{
    /// 컷씬 대사 영어판 — key "<sceneId>:<line index>" (Tools/Story/cutscene_txt.py import 로 생성). 영어 모드(Loc.IsKo == false)에서 ChapterVN이 사용.
    public static partial class ChapterScript
    {
        public static string SpeakerEn(string ko)
        {
            switch (ko)
            {
                case "하늘": return "Haneul";
                case "도윤": return "Doyun";
                case "루아": return "Rua";
                case "만수": return "Mansu";
                case "할머니": return "Grandma";
                case "라디오": return "Radio";
                case "DJ": return "DJ";
                case "아빠": return "Dad";
                case "엄마": return "Mom";
                case "바다": return "Bada";
                case "꼬마": return "Kid";
                case "아이들": return "Kids";
                case "큰 아저씨": return "Big Suit";
                case "마른 아저씨": return "Thin Suit";
                case "기사": return "Driver";
                default: return ko;
            }
        }

        /// 영어 텍스트. 없으면 null(한국어 유지).
        public static string TextEn(string sceneId, int index)
        {
            return En.TryGetValue(sceneId + ":" + index, out var s) ? s : null;
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // END_A_SENSE
            { "END_A_SENSE:1", "Want me to read it? If you read it, you'd read it late." },
            { "END_A_SENSE:2", "…Read it." },
            { "END_A_SENSE:3", "He unfolds the letter. Half sunset, half his voice." },
            { "END_A_SENSE:4", "'Haneul. I'm going. Fixed your trucks. The bushing.' …That part you already know." },
            { "END_A_SENSE:5", "'I like you. You can come late. I'll wait.' That part I've never said out loud." },
            { "END_A_SENSE:6", "Haneul doesn't answer. That not answering is the answer — after six years, they both know." },
            { "END_A_SENSE:7", "89.2. Two in the morning." },
            { "END_A_SENSE:8", "Yeah. There." },
            // END_A_TRUST
            { "END_A_TRUST:1", "Sound from the bottom of the hill. Mansu's truck. Grandma. Rua. The market people." },
            { "END_A_TRUST:2", "I brought bags. Bags. Put something in them to take along." },
            { "END_A_TRUST:3", "The Seo boy. This isn't tonic, it's just tangerines. Eat." },
            { "END_A_TRUST:4", "…This village, honestly." },
            { "END_A_TRUST:5", "Oppa. Leave late." },
            { "END_A_TRUST:6", "Long after the sun is gone, nobody leaves first. Today he didn't have to leave first." },
            { "END_A_TRUST:7", "I ran every day for a year, and today we all walk down together." },
            // END_B_TRUST
            { "END_B_TRUST:1", "No one is there. No — one person. Rua, sitting on the stone at the tower base." },
            { "END_B_TRUST:2", "He left a while ago. He saw you coming and left. You'd have seen only the back of his head." },
            { "END_B_TRUST:3", "…I did." },
            { "END_B_TRUST:4", "You weren't late all year. Everyone knows what you did around the village. He knew too." },
            { "END_B_TRUST:5", "Rua holds out the tin can. A note inside. Not read yet." },
            { "END_B_TRUST:6", "Let's read it together. It's been a while since I saw his handwriting too." },
            { "END_B_TRUST:7", "The radio stinger is the same. What comes after, the two of them listen to together." },
            // END_B_WEAK
            { "END_B_WEAK:1", "Three of the 4.2 kilometers. Her legs stop first. The last day, the last sunset." },
            { "END_B_WEAK:2", "…Not yet. Half the sunset is still left." },
            { "END_B_WEAK:3", "She sits down on the roadside. Where Doyun sat. Where the medicine bag fell." },
            { "END_B_WEAK:4", "The sun goes all the way down. The tower is visible from here too. No one is standing under it." },
            { "END_B_WEAK:5", "I never built my body up, all year. That's it. That's all it was." },
            { "END_B_WEAK:6", "Next spring she'll be able to run. She won't be late then. She decides to think that." },
            // SIDE_RUA_1
            { "SIDE_RUA_1:1", "Do you go up the oreum a lot? My brother said he saw you there." },
            { "SIDE_RUA_1:2", "…What did he say." },
            { "SIDE_RUA_1:3", "I won't tell you. Just — he says you look funny running, from far away." },
            { "SIDE_RUA_1:4", "Rua laughed for the first time. Like a thirteen-year-old." },
            // SIDE_RUA_2
            { "SIDE_RUA_2:1", "I'll tell only you. My brother's medicine bags — they're empty." },
            { "SIDE_RUA_2:2", "…What do you mean." },
            { "SIDE_RUA_2:3", "I don't know. I don't know either. They're just empty. He says it only works while you believe it." },
            { "SIDE_RUA_2:4", "Rua says nothing more. She closes the gate halfway." },
            // SIDE_RUA_3
            { "SIDE_RUA_3:1", "Every day he leaves first, he comes to me. He always says it's not because you were late." },
            { "SIDE_RUA_3:2", "…Why are you telling me." },
            { "SIDE_RUA_3:3", "When winter ends he goes. If you blame yourself then, that's what he'd hate most." },
            { "SIDE_RUA_3:4", "Run on the last day. That day he won't leave." },
            { "SIDE_RUA_3:5", "Rua taps the tin-can spot with her foot. Eleven steps. Right." },
            // SIDE_MANSU_1
            { "SIDE_MANSU_1:1", "You work well. I won't charge for the bag. The bag." },
            { "SIDE_MANSU_1:2", "…Thanks." },
            { "SIDE_MANSU_1:3", "That kid — he just looks at the bags and leaves. Every day. He's not buying. He's measuring time." },
            // SIDE_MANSU_2
            { "SIDE_MANSU_2:1", "Take this. Raincoat. New. The kid bought it and never took it." },
            { "SIDE_MANSU_2:2", "…Doyun did?" },
            { "SIDE_MANSU_2:3", "Bought it because a typhoon was coming. Not for himself. I'll put it in a bag. A bag." },
            { "SIDE_MANSU_2:4", "The raincoat is folded. Never once opened." },
            // SIDE_MANSU_3
            { "SIDE_MANSU_3:1", "I ran this store six years ago too. The day that kid left for Seoul, he bought a tin can here." },
            { "SIDE_MANSU_3:2", "…A tin can." },
            { "SIDE_MANSU_3:3", "Asked what he'd put in it — a note, he said. What note does a twelve-year-old write. But it's still there, I hear. Here, one more bag. A bag." },
            { "SIDE_MANSU_3:4", "Mansu starts to take something from under the counter, then stops. Not today, it seems." },
            // SIDE_GRANDMA_1
            { "SIDE_GRANDMA_1:1", "Your hands are yellow. Tangerine hands. Those are working hands." },
            { "SIDE_GRANDMA_1:2", "…It won't wash off." },
            { "SIDE_GRANDMA_1:3", "You don't wash it off. That boy's aren't yellow. He doesn't touch them. Only smells." },
            // SIDE_GRANDMA_2
            { "SIDE_GRANDMA_2:1", "My old man was sick too. I fed him everything the hospital forbade. He went anyway." },
            { "SIDE_GRANDMA_2:2", "…" },
            { "SIDE_GRANDMA_2:3", "You drink the tonic. The one who runs should. Tell that boy to just smell it." },
            { "SIDE_GRANDMA_2:4", "Grandma hands over one more bottle. This one is Haneul's." },
            // SIDE_GRANDMA_3
            { "SIDE_GRANDMA_3:1", "I asked at the shrine. Whether there are people who stay only one year and go." },
            { "SIDE_GRANDMA_3:2", "…What did they say." },
            { "SIDE_GRANDMA_3:3", "They said yes. Only at sunset. Just don't be late, they said. That's all." },
            { "SIDE_GRANDMA_3:4", "Take one more tangerine. Don't leave it on the stone — put it in his hand." },
            // SIDE_DJ_1
            { "SIDE_DJ_1:1", "Tonight's letter is from Jeju. You wrote again about the friend who's always late." },
            { "SIDE_DJ_1:2", "(That's mine.)" },
            { "SIDE_DJ_1:3", "Everyone listening at this hour is someone who was late. It's fine. The show doesn't end." },
            // SIDE_DJ_2
            { "SIDE_DJ_2:1", "There's a story about two people listening to the same frequency. One wrote two years ago, one last week." },
            { "SIDE_DJ_2:2", "…Two years ago." },
            { "SIDE_DJ_2:3", "We couldn't read the one from two years ago back then. They asked for spring. We'll read it in spring." },
            { "SIDE_DJ_2:4", "Haneul didn't turn the radio off. For the first time she listened to the end." },
            // SIDE_DJ_3
            { "SIDE_DJ_3:1", "Jeju listener. This time you sent a request instead of a story. Play this song at two in the morning on the last day." },
            { "SIDE_DJ_3:2", "We'll play it. Someone else will be listening at that hour too." },
            { "SIDE_DJ_3:3", "…I never sent a request." },
            { "SIDE_DJ_3:4", "Haneul knows who sent it. 89.2. Through the static, the first-snow song begins." },
        };
    }
}
