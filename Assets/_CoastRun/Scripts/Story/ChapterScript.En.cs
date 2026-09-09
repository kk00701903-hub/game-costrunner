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
            { "PRO:1", "(Prologue — placeholder)" },
            // CH01_Open
            { "CH01_Open:1", "(Chapter 1 opening — placeholder)" },
            { "CH01_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH01_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH01_Close
            { "CH01_Close:1", "(Chapter 1 close — placeholder)" },
            // CH02_Open
            { "CH02_Open:1", "(Chapter 2 opening — placeholder)" },
            { "CH02_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH02_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH02_Close
            { "CH02_Close:1", "(Chapter 2 close — placeholder)" },
            // CH03_Open
            { "CH03_Open:1", "(Chapter 3 opening — placeholder)" },
            { "CH03_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH03_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH03_Close
            { "CH03_Close:1", "(Chapter 3 close — placeholder)" },
            // CH04_Open
            { "CH04_Open:1", "(Chapter 4 opening — placeholder)" },
            { "CH04_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH04_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH04_Close
            { "CH04_Close:1", "(Chapter 4 close — placeholder)" },
            // CH05_Open
            { "CH05_Open:1", "(Chapter 5 opening — placeholder)" },
            { "CH05_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH05_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH05_Close
            { "CH05_Close:1", "(Chapter 5 close — placeholder)" },
            // CH06_Open
            { "CH06_Open:1", "(Chapter 6 opening — placeholder)" },
            { "CH06_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH06_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH06_Close
            { "CH06_Close:1", "(Chapter 6 close — placeholder)" },
            // CH07_Open
            { "CH07_Open:1", "(Chapter 7 opening — placeholder)" },
            { "CH07_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH07_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH07_Close
            { "CH07_Close:1", "(Chapter 7 close — placeholder)" },
            // CH08_Open
            { "CH08_Open:1", "(Chapter 8 opening — placeholder)" },
            { "CH08_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH08_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH08_Close
            { "CH08_Close:1", "(Chapter 8 close — placeholder)" },
            // CH09_Open
            { "CH09_Open:1", "(Chapter 9 opening — placeholder)" },
            { "CH09_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH09_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH09_Close
            { "CH09_Close:1", "(Chapter 9 close — placeholder)" },
            // CH10_Open
            { "CH10_Open:1", "(Chapter 10 opening — placeholder)" },
            { "CH10_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH10_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH10_Close
            { "CH10_Close:1", "(Chapter 10 close — placeholder)" },
            // CH11_Open
            { "CH11_Open:1", "(Chapter 11 opening — placeholder)" },
            { "CH11_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH11_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH11_Close
            { "CH11_Close:1", "(Chapter 11 close — placeholder)" },
            // CH12_Open
            { "CH12_Open:1", "(Chapter 12 opening — placeholder)" },
            { "CH12_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH12_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH12_Close
            { "CH12_Close:1", "(Chapter 12 close — placeholder)" },
            // CH13_Open
            { "CH13_Open:1", "(Chapter 13 opening — placeholder)" },
            { "CH13_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH13_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH13_Close
            { "CH13_Close:1", "(Chapter 13 close — placeholder)" },
            // CH14_Open
            { "CH14_Open:1", "(Chapter 14 opening — placeholder)" },
            { "CH14_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH14_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH14_Close
            { "CH14_Close:1", "(Chapter 14 close — placeholder)" },
            // CH15_Open
            { "CH15_Open:1", "(Chapter 15 opening — placeholder)" },
            { "CH15_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH15_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH15_Close
            { "CH15_Close:1", "(Chapter 15 close — placeholder)" },
            // CH16_Open
            { "CH16_Open:1", "(Chapter 16 opening — placeholder)" },
            { "CH16_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH16_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH16_Close
            { "CH16_Close:1", "(Chapter 16 close — placeholder)" },
            // CH17_Open
            { "CH17_Open:1", "(Chapter 17 opening — placeholder)" },
            { "CH17_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH17_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH17_Close
            { "CH17_Close:1", "(Chapter 17 close — placeholder)" },
            // CH18_Open
            { "CH18_Open:1", "(Chapter 18 opening — placeholder)" },
            { "CH18_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH18_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH18_Close
            { "CH18_Close:1", "(Chapter 18 close — placeholder)" },
            // CH19_Open
            { "CH19_Open:1", "(Chapter 19 opening — placeholder)" },
            { "CH19_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH19_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH19_Close
            { "CH19_Close:1", "(Chapter 19 close — placeholder)" },
            // CH20_Open
            { "CH20_Open:1", "(Chapter 20 opening — placeholder)" },
            { "CH20_Open:2", "[게이트>=0]…I think I can run today." },
            { "CH20_Open:3", "[게이트<0]…My legs aren't ready. One more week." },
            // CH20_Close
            { "CH20_Close:1", "(Chapter 20 close — placeholder)" },
            // END_A
            { "END_A:1", "(Ending A — placeholder)" },
            // END_B
            { "END_B:1", "(Ending B — placeholder)" },
        };
    }
}
