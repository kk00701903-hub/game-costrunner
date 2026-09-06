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
            // PRO
            { "PRO:1", "\"I came back down. Under the tower when the sun sets. Let's see if our base is still there.\"" },
            { "PRO:2", "…Did he lose his phone?" },
            { "PRO:4", "Display board: Under maintenance · Service suspended." },
            { "PRO:5", "…4.2 kilometers." },
            { "PRO:6", "She flips the board over." },
            // CH01_Open
            { "CH01_Open:1", "Need a bag? A bag." },
            { "CH01_Open:2", "…The Seo family. Next door. I heard their son came back." },
            { "CH01_Open:3", "Ah, the sick kid. Came from Seoul to recover, they say. He was just here. Just now." },
            { "CH01_Open:4", "…What did he buy?" },
            { "CH01_Open:5", "Nothing. Just looked at the bags and left. The bags." },
            { "CH01_Open:6", "…I feel like we've had this conversation before." },
            { "CH01_Open:7", "You've been at the village hall a lot lately. Here, one more bag." },
            // CH01_Close
            { "CH01_Close:1", "…Doyun." },
            { "CH01_Close:2", "He doesn't turn around. The silver grass closes." },
            { "CH01_Close:3", "A note where he sat. \"You're late. Tomorrow, before sunset.\"" },
            { "CH01_Close:4", "She only saw the back of his head. She still knows." },
            { "CH01_Close:5", "She only saw the back of his head, and still knew his steps hurt." },
            // CH02_Open
            { "CH02_Open:1", "She went in daytime. Without waiting for the sunset. He was digging." },
            { "CH02_Open:2", "Oh, you came in the day. No, that's not — it's not here, a bit more south. You dug and I covered, so count with your steps. Twelve?" },
            { "CH02_Open:3", "Eleven." },
            { "CH02_Open:4", "Right, eleven. But those were twelve-year-old steps." },
            { "CH02_Open:5", "The tin can doesn't turn up. He coughs. Haneul pretends not to hear." },
            { "CH02_Open:6", "Why did you just leave yesterday?" },
            { "CH02_Open:7", "The sun was all the way down. No, that's not the reason — it was cold." },
            { "CH02_Open:8", "…Your phone?" },
            { "CH02_Open:9", "Left it at the hospital. They don't let you use it there." },
            { "CH02_Open:10", "You've gotten better on the board. Definitely better than at twelve." },
            { "CH02_Open:11", "(A few steps and I'm out of breath. How am I going to make the sunset like this.)" },
            { "CH02_Open:12", "…Eleven steps. You said it first. No — you're going to say it." },
            // CH02_Close
            { "CH02_Close:1", "Tomorrow again?" },
            { "CH02_Close:2", "…Tomorrow's the hospital." },
            // CH03_Open
            { "CH03_Open:1", "The radio. The frequency he wrote on the shovel handle. One digit smeared in the dirt." },
            { "CH03_Open:2", "Ninety-one point… four? Seven?" },
            { "CH03_Open:3", "Static. Commercials. Trot songs." },
            { "CH03_Open:4", "(The digit isn't smeared by accident. It looks smeared on purpose.)" },
            { "CH03_Open:5", "(This DJ's voice — the one I hear every night at two.)" },
            // CH03_Close
            { "CH03_Close:1", "Nothing. Chalk on the stone. \"91.9 — two in the morning. Not in the daytime.\"" },
            { "CH03_Close:2", "The chalk is wet but the writing hasn't run." },
            { "CH03_Close:3", "…Who listens to the radio at two in the morning?" },
            { "CH03_Close:4", "That night at two, she listened. The DJ read a story. Someone else's story. She listened to the end anyway." },
            // CH04_Open
            { "CH04_Open:1", "My brother? My brother said he's meeting someone?" },
            { "CH04_Open:2", "…Me." },
            { "CH04_Open:3", "You're Haneul, the one he talks about?" },
            { "CH04_Open:4", "…Yeah. Is Doyun in?" },
            { "CH04_Open:5", "He's not here right now." },
            { "CH04_Open:6", "…The hospital?" },
            { "CH04_Open:7", "Why are you asking me that?" },
            { "CH04_Open:8", "Through the door gap, the shoe rack. One pair of sneakers, one pair of adult shoes. That's all." },
            { "CH04_Open:9", "Is the little sister guarding him, Haneul thinks. At thirteen." },
            { "CH04_Open:10", "…You can come in. He's not here, though." },
            { "CH04_Open:11", "People in the village don't say nice things about you. That's why I'm asking." },
            // CH04_Close
            { "CH04_Close:1", "Nothing. The grass is pressed down. In the shape of someone who was sitting there a moment ago." },
            { "CH04_Close:2", "It takes ten minutes to untangle the cord. She felt sorry. She doesn't know why." },
            // CH05_Open
            { "CH05_Open:1", "They ran into each other on the coastal road. No promise made. The canola has all fallen." },
            { "CH05_Open:2", "Oh. Fancy seeing you here." },
            { "CH05_Open:3", "…You keep not being there." },
            { "CH05_Open:4", "Me? No, it's not that I'm not there, it's that you're late. Your trucks are loose, that's why. The kingpin nut — no, it's the bushing. The bushing's gone." },
            { "CH05_Open:5", "It was like that when we were twelve too." },
            { "CH05_Open:6", "Misdiagnosed it back then. Took six years." },
            { "CH05_Open:7", "…Five." },
            { "CH05_Open:8", "Six. I counted right." },
            { "CH05_Open:9", "Your trucks don't wobble anymore. You fixed them?" },
            { "CH05_Open:10", "(I haven't been sleeping. Is that why I'm late.)" },
            { "CH05_Open:11", "Spring ending — this time I'll say it first. Go ahead." },
            // CH05_Close
            { "CH05_Close:1", "A medicine bag falls. It makes no sound." },
            { "CH05_Close:2", "…Spring's over. Go ahead. Before sunset." },
            { "CH05_Close:3", "Together —" },
            { "CH05_Close:4", "I'm slow. Go ahead." },
            { "CH05_Close:5", "Haneul went first. When she reached the tower half the sunset was left, and he didn't come." },
            // CH06_Open
            { "CH06_Open:1", "They promised by note. \"The festival. You can see the fireworks from the tower.\" The reply: \"Crowds, not really. No, it's not that I don't like them — okay.\"" },
            { "CH06_Open:2", "The old town. Gongs. Across the crowd, there he is. Under the lanterns." },
            { "CH06_Open:3", "Doyun!" },
            { "CH06_Open:4", "He looks. Three seconds. The drum troupe passes. When it's gone, he's gone." },
            { "CH06_Open:5", "She looks at the photo she took on her phone. His spot is eaten by lantern light, blown out white. Backlight, probably." },
            { "CH06_Open:6", "(People keep talking to me. No time to look for the back of his head.)" },
            { "CH06_Open:7", "Mansu's stall. Bags hanging like lanterns." },
            // CH06_Close
            { "CH06_Close:1", "Fireworks. She watches alone. At her feet, one festival lantern. Lit." },
            { "CH06_Close:2", "She got three seconds. That's what she lived on all summer." },
            // CH07_Open
            { "CH07_Open:1", "Daytime. Hamdeok. This time he was there first. Only his feet in the water." },
            { "CH07_Open:2", "Seoul has no sea. The Han River isn't the sea." },
            { "CH07_Open:3", "I know." },
            { "CH07_Open:4", "What do you know, you've never even been." },
            { "CH07_Open:5", "…Why did you leave at the festival?" },
            { "CH07_Open:6", "Too many people. No, that's not it — I saw you coming, but the sunset was — I just left. Sorry." },
            { "CH07_Open:7", "\"Sorry\" was a first." },
            { "CH07_Open:8", "You've been listening to the radio. Your voice sounds like two in the morning." },
            { "CH07_Open:9", "(My feet don't hurt. I ran all summer.)" },
            // CH07_Close
            { "CH07_Close:1", "You got burned, right? Me too." },
            { "CH07_Close:2", "…I didn't." },
            { "CH07_Close:3", "You did. The hospital's going to say something." },
            { "CH07_Close:4", "They reached it before sunset. Together, for the first time. This chapter is all of summer." },
            // CH08_Open
            { "CH08_Open:1", "The day Doyun called \"the big hospital day.\" Rua is wearing a backpack. Doyun isn't there." },
            { "CH08_Open:2", "Where's Doyun?" },
            { "CH08_Open:3", "…Did my brother tell you he's going to the hospital?" },
            { "CH08_Open:4", "Yeah. Today." },
            { "CH08_Open:5", "Then I guess that's what it is." },
            { "CH08_Open:6", "The bus comes. Only Rua gets on. Window seat. The seat beside her is empty." },
            { "CH08_Open:7", "Haneul runs after the bus. Two stops. At the third, the bus pulls away." },
            { "CH08_Open:8", "…I'll tell you. It's not the hospital." },
            { "CH08_Open:9", "(The bus driver sees me and closes the door late.)" },
            // CH08_Close
            { "CH08_Close:1", "She looks for the tin can and gives up. It doesn't feel like eleven steps." },
            // CH09_Open
            { "CH09_Open:1", "Glass is going out. The glass." },
            { "CH09_Open:2", "The note from under the tower stone yesterday. \"The tower won't fall, right? Our tin can.\"" },
            { "CH09_Open:3", "…And if it falls?" },
            { "CH09_Open:4", "What?" },
            { "CH09_Open:5", "Nothing." },
            { "CH09_Open:6", "He was here a while ago, that kid. Bought a raincoat. A raincoat." },
            { "CH09_Open:7", "…When?" },
            { "CH09_Open:8", "A while ago." },
            { "CH09_Open:9", "\"A while ago,\" for Mansu, can mean ten minutes or three hours." },
            { "CH09_Open:10", "That raincoat — I gave it to you, right. The kid bought it and left it." },
            { "CH09_Open:11", "(Why didn't I believe the tower wouldn't fall.)" },
            // CH09_Close
            { "CH09_Close:1", "The note she left last night — \"It didn't fall.\" — is gone. Instead: \"Told you.\"" },
            { "CH09_Close:2", "The raincoat is folded, never once opened. The paper is dry." },
            // CH10_Open
            { "CH10_Open:1", "Two in the morning. 91.9." },
            { "CH10_Open:2", "Next story… from an eighteen-year-old listener recovering on Jeju. Says the friend next door keeps being late." },
            { "CH10_Open:3", "She turns it off. Turns it on." },
            { "CH10_Open:4", "— there's only one year, and every day that kid is late he loses a day, so he gets up and leaves first. I'm not sure what it means either…" },
            { "CH10_Open:5", "She turns it off." },
            { "CH10_Open:6", "(I sent a story twice. The DJ seems to know my handwriting.)" },
            { "CH10_Open:7", "'There's only one year.' I already know this line." },
            // CH10_Close
            { "CH10_Close:1", "He can't come out today. He said not to tell you." },
            { "CH10_Close:2", "…Was he admitted?" },
            { "CH10_Close:3", "Did I say that?" },
            { "CH10_Close:4", "Then what is it?" },
            { "CH10_Close:5", "I'd rather not say either. Is that not allowed?" },
            // CH11_Open
            { "CH11_Open:1", "The tangerine farm job. He's sitting on the ridge between rows. Said he was discharged. Thinner, but he still talks in long sentences." },
            { "CH11_Open:2", "You peel a tangerine from the stem end so it doesn't burst. No, that's a mandarin, this is… what is this." },
            { "CH11_Open:3", "Just a tangerine." },
            { "CH11_Open:4", "There's no such thing as just a tangerine." },
            { "CH11_Open:5", "She peels one and holds it out. He only holds it." },
            { "CH11_Open:6", "Later. Just the smell for now." },
            { "CH11_Open:7", "The grandmother who owns the farm comes over with a bottle of tonic." },
            { "CH11_Open:8", "Heard the Seo boy is sick. Drink this. It's what my old man used to take." },
            { "CH11_Open:9", "Thank you. But the hospital said not to take anything else —" },
            { "CH11_Open:10", "What does a hospital know." },
            { "CH11_Open:11", "Haneul worked three more hours until the shift ended. When she looked back, the ridge was empty." },
            { "CH11_Open:12", "That kid? He just left. Said the sun was setting. Why does a sick boy fuss so much over sunsets." },
            { "CH11_Open:13", "Look at your yellow hands. And that boy's are white." },
            { "CH11_Open:14", "I heard you deliver at night. Mansu's worried." },
            { "CH11_Open:15", "(The crates are heavy. Three hours is long.)" },
            // CH11_Close
            { "CH11_Close:1", "Nothing. The tangerine and the tonic sit on the stone. As if only smelled." },
            { "CH11_Close:2", "Only Haneul's hands are yellow." },
            // CH12_Open
            { "CH12_Open:1", "This time they met in the morning. Avoiding the sunset. Haneul decided that." },
            { "CH12_Open:2", "You go up first. Take a picture from the top and show me." },
            { "CH12_Open:3", "…It's meaningless if we don't go together." },
            { "CH12_Open:4", "It doesn't need meaning. A picture is enough. This is as far as I can go. No, not as far as I can go — as far as I go today." },
            { "CH12_Open:5", "Then the tower. I'll climb up there and take it. Wait at the bottom." },
            { "CH12_Open:6", "…Okay. At the bottom." },
            { "CH12_Open:7", "Aren't you scared of climbing the tower? No, you wouldn't be." },
            { "CH12_Open:8", "Rua is watching from far away. She doesn't wave." },
            // CH12_Close
            { "CH12_Close:1", "He's at the bottom. Waving. He's in the picture — no, when she comes down and looks, only his spot is out of focus." },
            { "CH12_Close:2", "This is better." },
            { "CH12_Close:3", "You're not even in it." },
            { "CH12_Close:4", "That's why it's better." },
            // CH13_Open
            { "CH13_Open:1", "What will you do when my brother goes?" },
            { "CH13_Open:2", "…Goes? Where?" },
            { "CH13_Open:3", "I asked you." },
            { "CH13_Open:4", "Haneul doesn't answer and stands up. From that week she stops going to the tower. One day, two, four." },
            { "CH13_Open:5", "If you stop chasing, there's nothing to miss. That's what she thought." },
            { "CH13_Open:6", "That kid comes every day. Looks at the bags and leaves. Every day." },
            { "CH13_Open:7", "…So?" },
            { "CH13_Open:8", "So." },
            { "CH13_Open:9", "Where he's going isn't Seoul. I don't know where it is either." },
            { "CH13_Open:10", "The kid came today too. Just looked at the bags. That's eleven times." },
            // CH13_Close
            { "CH13_Close:1", "She knows who left it. She still doesn't go the next day. Or the day after." },
            // CH14_Open
            { "CH14_Open:1", "She went in the day. He was there first. Today his sentences are short." },
            { "CH14_Open:2", "I had something to say." },
            { "CH14_Open:3", "…Yeah." },
            { "CH14_Open:4", "Forgot it." },
            { "CH14_Open:5", "They both know it's a lie. Haneul could have asked first. She didn't. Wind. Silver grass. Haneul counts numbers. Twelve percent battery left. The first time they'd met in ten days, and it wasn't even five minutes. In those five minutes she only had to ask one of three things — why he keeps getting thinner, where the hospital is, where he goes in winter — and she couldn't ask any of them, and she never knew it could be that hard." },
            { "CH14_Open:6", "…I'll go first. You stay today." },
            { "CH14_Open:7", "He went first. It's the first time. That Haneul is the one who stays." },
            { "CH14_Open:8", "I had something to say. …You do too. It's on your face." },
            { "CH14_Open:9", "(Twelve percent. The battery, and me.)" },
            { "CH14_Open:10", "This time I didn't forget. I just can't. I'll say it in winter." },
            // CH15_Open
            { "CH15_Open:1", "Rua comes to Haneul's house. A first. Carrying a succulent in a pot." },
            { "CH15_Open:2", "My brother leaves when winter ends. That part's decided." },
            { "CH15_Open:3", "…Where to? A hospital in Seoul?" },
            { "CH15_Open:4", "…If that's what you think, then that's what it is." },
            { "CH15_Open:5", "When?" },
            { "CH15_Open:6", "Why do you ask me that? He thinks you've stopped chasing him, you know." },
            { "CH15_Open:7", "She sets the pot on the porch and leaves. She said it has no name." },
            { "CH15_Open:8", "The succulent has no name. You name it. He said so." },
            { "CH15_Open:9", "(Succulents don't die in winter. That's why she gave it.)" },
            // CH15_Close
            { "CH15_Close:1", "This time it comes out. Eleven steps was right. She doesn't open it. She doesn't want to open it alone." },
            // CH16_Open
            { "CH16_Open:1", "They ran into each other at the bus stop. Snow. He's wearing two scarves." },
            { "CH16_Open:2", "It's different from Seoul snow." },
            { "CH16_Open:3", "How?" },
            { "CH16_Open:4", "…Don't know. It's different. Seoul snow turns gray as soon as it lands, but here — no, that's not it. It's just different." },
            { "CH16_Open:5", "Come with me. Before sunset today." },
            { "CH16_Open:6", "…Okay." },
            { "CH16_Open:7", "Grandma gave me tonic too. Told me to just smell it." },
            { "CH16_Open:8", "(Snow on the road and I'm not slipping. A year's worth of legs.)" },
            // CH16_Close
            { "CH16_Close:1", "They reached it before sunset. The two of them. For the first time Haneul finishes a sentence to the end." },
            { "CH16_Close:2", "This is nice." },
            { "CH16_Close:3", "…Yeah. It is." },
            // CH17_Open
            { "CH17_Open:1", "A note under the stone. \"Can't come out this week. Tests. Big ones. No, not big — long ones. Open the tin can. It's yours. Half of it.\"" },
            { "CH17_Open:2", "…What tests?" },
            { "CH17_Open:3", "The note doesn't answer." },
            { "CH17_Open:4", "(The tin can Mansu talked about. Bought there six years ago.)" },
            { "CH17_Open:5", "(I've already read this note. My hands still shake.)" },
            // CH17_Close
            { "CH17_Close:1", "The things they put in at twelve. Stickers. A front tooth. And the note he put in the day before he left for Seoul." },
            { "CH17_Close:2", "\"I'll come back. Radio 91.9, two in the morning. Meet me there.\"" },
            { "CH17_Close:3", "For six years she didn't know. So it wasn't only for treatment that he chose that hospital, she thought. That was a misunderstanding too." },
            // CH18_Open
            { "CH18_Open:1", "Two in the morning. This time she gets it on the first try. Haneul sent in her story three days ago. For the first time, writing a sentence all the way to the end." },
            { "CH18_Open:2", "A story from Jeju. You say you're finally answering a note you got six years ago. …They'll be listening too. Everyone listening at this hour is." },
            { "CH18_Open:3", "And one more. A story sent two springs ago from a hospital in Seoul, by an eighteen-year-old listener. We couldn't read it back then. \"Next spring I'm going back to Jeju. To the kid next door — \"" },
            { "CH18_Open:4", "Two springs ago." },
            { "CH18_Open:5", "She listens to the end. She listened to the end, but she can't remember the rest." },
            { "CH18_Open:7", "(The DJ reads my request. One I never sent.)" },
            { "CH18_Open:8", "(The story from two years ago — I remember it to the end.)" },
            // CH18_Close
            { "CH18_Close:1", "Rua brings Haneul home. Doyun's room. The bed is made and there are no belongings. On the desk, one photo frame, face down." },
            { "CH18_Close:2", "My brother died two springs ago. At the hospital in Seoul." },
            { "CH18_Close:3", "…" },
            { "CH18_Close:4", "This spring he walked in through the gate. Mom couldn't see him. Only I could. The village thinks he came back sick. That's what he went around saying. To you too." },
            { "CH18_Close:5", "The medicine bags." },
            { "CH18_Close:6", "Empty. He said as long as you believe the illness, he can stay. When winter ends, he goes. That's all I know." },
            { "CH18_Close:7", "…You kept saying the hospital." },
            { "CH18_Close:8", "I never once said hospital. That's what you heard. Every time." },
            { "CH18_Close:9", "Rua stands the frame up. Eighteen. All his front teeth grown in." },
            { "CH18_Close:10", "Every day you were late, my brother left first. So you wouldn't blame yourself. Did you know that?" },
            { "CH18_Close:11", "Haneul can't answer. It was sad. There's no other word. And from that day, Haneul ran every day." },
            // CH19_Open
            { "CH19_Open:1", "She reached it before sunset. He's there. Today he talks at length. From beginning to end." },
            { "CH19_Open:2", "Rua told you, didn't she. She can't hold anything in. Even at twelve she told everyone where the tin can was — no, that's not the point." },
            { "CH19_Open:3", "…What's the rule?" },
            { "CH19_Open:4", "One year. From the first day of spring to the last day of winter. The sunset is the clock. I have to be at the tower when the sun sets. Because this is the antenna. No, not an antenna — it just comes in clearest here. Once the sun's down, I can't stay." },
            { "CH19_Open:5", "That's why you kept leaving first." },
            { "CH19_Open:6", "Every day you're late, I lose a day. That's the rule. But if you knew, you'd run every day, and you'd get hurt running — so on the days you were late I just left first. So it wouldn't be your fault." },
            { "CH19_Open:7", "That's worse." },
            { "CH19_Open:8", "…I know. No, I didn't. I know now." },
            { "CH19_Open:9", "He touches the envelope. He doesn't hand it over." },
            { "CH19_Open:10", "The last day of winter. When the sun sets. Come then. That day I won't leave first." },
            { "CH19_Open:11", "…How many days left. Exactly." },
            { "CH19_Open:12", "Eight on the calendar. Minus the days you were late — but that's my count to keep." },
            { "CH19_Open:13", "Doyun." },
            { "CH19_Open:14", "Yeah." },
            { "CH19_Open:15", "I'll come. I won't be late. This time I'm the one chasing." },
            { "CH19_Open:16", "She finished the sentence to the end. The second time." },
            { "CH19_Open:17", "The whole village knows. That you run every day. Some days I couldn't leave first because of that." },
            { "CH19_Open:18", "…You look pale lately. On the last day, you can walk instead of run." },
            { "CH19_Open:19", "This is the second time I'm telling you the rule. This time you knew first." },
            // CH19_Close
            { "CH19_Close:1", "A polar bear. It doesn't mean anything." },
            // CH20_Open
            { "CH20_Open:1", "The last day of winter. There's no bus. The canola is a day early." },
            { "CH20_Open:2", "…4.2 kilometers." },
            { "CH20_Open:3", "She flips the board over. The twentieth time." },
            // END_A
            { "END_A:1", "You're late." },
            { "END_A:2", "The bus." },
            { "END_A:3", "I know. You're not late. Half the sunset's still left. First time in a year you got here first." },
            { "END_A:5", "Haneul" },
            { "END_A:6", "I'm going" },
            { "END_A:7", "Fixed your trucks. The bushing." },
            { "END_A:8", "I like you" },
            { "END_A:9", "You can come late. I'll wait" },
            { "END_A:11", "She finishes reading and looks up. The sun is all the way down. No one is there." },
            { "END_A:12", "This time it wasn't a miss. She saw him off. She never got to say goodbye, but he said she could come late. It didn't say where he'd wait, but she knows. 91.9." },
            { "END_A:14", "Two in the morning. A story from Jeju. The one who sent one last year too… this time it's short. \"You're listening, right?\" …Yes. They'll be listening." },
            { "END_A:15", "Static. A stinger. Title card → credits. Skateboard character unlocked." },
            { "END_A:16", "(Second spring. This time I came with more than half the sunset left.)" },
            // END_B
            { "END_B:1", "No one is there. The canola bloomed a day early. As many days as Haneul was late, he ended that many days sooner. The twenty-first miss." },
            { "END_B:3", "Haneul" },
            { "END_B:4", "I meant not to leave first on the last day, but the day left first. Not your fault." },
            { "END_B:5", "Never did tune the frequency all the way. That was me." },
            { "END_B:6", "I'm burying this. Take care" },
            { "END_B:8", "Back at the bus stop the display has changed. Service resumed." },
            { "END_B:9", "The radio stinger is the same. What comes after, it doesn't say." },
            { "END_B:10", "(Rua is watching from somewhere. Today I'm not alone.)" },
            // END_A_SENSE
            { "END_A_SENSE:1", "Want me to read it? If you read it, you'd read it late." },
            { "END_A_SENSE:2", "…Read it." },
            { "END_A_SENSE:3", "He unfolds the letter. Half sunset, half his voice." },
            { "END_A_SENSE:4", "'Haneul. I'm going. Fixed your trucks. The bushing.' …That part you already know." },
            { "END_A_SENSE:5", "'I like you. You can come late. I'll wait.' That part I've never said out loud." },
            { "END_A_SENSE:6", "Haneul doesn't answer. That not answering is the answer — after six years, they both know." },
            { "END_A_SENSE:7", "91.9. Two in the morning." },
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
            // END_TRUE
            { "END_TRUE:1", "The following spring. Two in the morning. 91.9." },
            { "END_TRUE:2", "Tonight it came not as a letter but as a voice. From Jeju, nineteen years old…" },
            { "END_TRUE:3", "Static. Then a voice she knows. The voice of someone who talks in long sentences." },
            { "END_TRUE:4", "Haneul. There was one more rule. No, not a rule — if you come all the way to the end twice, I get to say one more thing." },
            { "END_TRUE:5", "I said you could come late. Take that back. Don't be late. I'll be at that spot next spring too. When the sun sets." },
            { "END_TRUE:6", "Haneul doesn't turn the radio off. One light is on at the tower outside the window." },
            { "END_TRUE:7", "…Yeah. I won't be late." },
            { "END_TRUE:8", "And the canola blooms a day early." },
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
            { "SIDE_DJ_3:4", "Haneul knows who sent it. 91.9. Through the static, the first-snow song begins." },
        };
    }
}
