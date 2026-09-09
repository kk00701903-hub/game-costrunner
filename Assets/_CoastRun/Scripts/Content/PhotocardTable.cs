using UnityEngine;

namespace CoastRun
{
    /// 포토카드 30장. 가챠 없음 — 전부 조건 획득. 챕터 카드는 S급이면 '사인 버전'으로 승급.
    ///   01~20 챕터 카드(클리어) / 21~24 만남 카드(5·10·15·20 S급) / 25~28 시크릿 / 29~30 엔딩 A·B
    /// 그림: Resources/CoastRun/Card/Card_nn.png (2:3). 없으면 챕터 CG로 대체.
    public enum CardKind { Chapter, Meet, Secret, Ending }
    /// 38차: 포토카드 등급 — 드롭 확률 N 55 / R 30 / SR 12 / SSR 3 (%)
    public enum CardGrade { N = 0, R = 1, SR = 2, SSR = 3 }

    public struct CardDef
    {
        public int id;               // 1..30
        public CardKind kind;
        public int chapter;          // 챕터 카드/만남 카드의 챕터, 그 외 0
        public string ko, en;        // 카드 이름
        public string hintKo, hintEn;// 획득 조건 힌트
        public string backKo, backEn;// 뒷면 손글씨
        public CardDef(int id, CardKind kind, int ch, string ko, string en, string hintKo, string hintEn, string backKo, string backEn)
        { this.id = id; this.kind = kind; chapter = ch; this.ko = ko; this.en = en; this.hintKo = hintKo; this.hintEn = hintEn; this.backKo = backKo; this.backEn = backEn; }
        public string Name => Loc.T(ko, en);
        public string Hint => Loc.T(hintKo, hintEn);
        public string Back => Loc.T(backKo, backEn);
        public string Image => $"Card/Card_{id:00}";
        public string FallbackImage => chapter > 0 ? "BG_TowerSunset" : "BG_TowerNight";   // 34차: 옛 컷씬 그림(Cut_*) 대신 배경
    }

    public static class PhotocardTable
    {
        public const int Count = 30;

        public static readonly CardDef[] Cards =
        {
            new CardDef(1, CardKind.Chapter, 1, "무지개 해안도로", "Rainbow Coastal Road", "1챕터 클리어", "Clear chapter 1", "4.2킬로. 뛰었다.", "4.2 km. I ran."),
            new CardDef(2, CardKind.Chapter, 2, "목마등대", "Horse Lighthouse", "2챕터 클리어", "Clear chapter 2", "기지는 아직 있었다.", "The base was still there."),
            new CardDef(3, CardKind.Chapter, 3, "정낭 대문", "Jeongnang Gate", "3챕터 클리어", "Clear chapter 3", "91.9. 누가 말하는 것 같았다.", "91.9. Someone was talking."),
            new CardDef(4, CardKind.Chapter, 4, "영등굿 마을", "Yeongdeung Village", "4챕터 클리어", "Clear chapter 4", "루아는 서울 말을 쓴다.", "Rua talks like Seoul."),
            new CardDef(5, CardKind.Chapter, 5, "유채꽃밭", "Canola Field", "5챕터 클리어", "Clear chapter 5", "봄이 끝났다.", "Spring ended."),
            new CardDef(6, CardKind.Chapter, 6, "이호테우 밤 축제", "Iho Tewoo Night", "6챕터 클리어", "Clear chapter 6", "빙떡 하나 남겼다.", "I saved one bingtteok."),
            new CardDef(7, CardKind.Chapter, 7, "성산 해녀 탈의장", "Seongsan Shore", "7챕터 클리어", "Clear chapter 7", "발등이 빨갛다.", "My feet got sunburned."),
            new CardDef(8, CardKind.Chapter, 8, "동문시장", "Dongmun Market", "8챕터 클리어", "Clear chapter 8", "고기국수 두 그릇.", "Two bowls of meat noodles."),
            new CardDef(9, CardKind.Chapter, 9, "우도 원담", "Udo Wondam", "9챕터 클리어", "Clear chapter 9", "태풍 뒤에 바다가 조용했다.", "The sea was quiet after the typhoon."),
            new CardDef(10, CardKind.Chapter, 10, "곶자왈", "Gotjawal", "10챕터 클리어", "Clear chapter 10", "사연을 보냈다.", "I sent in a story."),
            new CardDef(11, CardKind.Chapter, 11, "성읍 귤밭", "Seongeup Tangerines", "11챕터 클리어", "Clear chapter 11", "껍질 안 깐 귤.", "A tangerine, unpeeled."),
            new CardDef(12, CardKind.Chapter, 12, "다랑쉬오름", "Darangshi Oreum", "12챕터 클리어", "Clear chapter 12", "말이 길을 막았다.", "A pony blocked the road."),
            new CardDef(13, CardKind.Chapter, 13, "메밀밭", "Buckwheat Field", "13챕터 클리어", "Clear chapter 13", "서울 얘기는 짧았다.", "The Seoul talk was short."),
            new CardDef(14, CardKind.Chapter, 14, "만장굴", "Manjanggul", "14챕터 클리어", "Clear chapter 14", "못 한 말이 있다.", "There are words I didn't say."),
            new CardDef(15, CardKind.Chapter, 15, "송악산 억새", "Songaksan Silver Grass", "15챕터 클리어", "Clear chapter 15", "낙엽 위에 발자국.", "Footprints on fallen leaves."),
            new CardDef(16, CardKind.Chapter, 16, "본향당 첫눈", "First Snow at the Shrine", "16챕터 클리어", "Clear chapter 16", "첫눈에 누가 이름을 불렀다.", "Someone called my name in the first snow."),
            new CardDef(17, CardKind.Chapter, 17, "방사탑", "Bangsatap", "17챕터 클리어", "Clear chapter 17", "돌 세 개.", "Three stones."),
            new CardDef(18, CardKind.Chapter, 18, "동백 숲", "Camellia Forest", "18챕터 클리어", "Clear chapter 18", "구십일 점 구.", "Ninety-one point nine."),
            new CardDef(19, CardKind.Chapter, 19, "산방산", "Sanbangsan", "19챕터 클리어", "Clear chapter 19", "규칙이 그래.", "That's the rule."),
            new CardDef(20, CardKind.Chapter, 20, "송전탑", "The Tower", "20챕터 클리어", "Clear chapter 20", "마지막 노을.", "The last sunset."),
            new CardDef(21, CardKind.Meet, 5, "봄 — 둘이서", "Spring — Together", "5챕터 S급", "Chapter 5 with rank S", "봄에 한 번 만났다.", "We met once in spring."),
            new CardDef(22, CardKind.Meet, 10, "여름 — 둘이서", "Summer — Together", "10챕터 S급", "Chapter 10 with rank S", "여름엔 방금 갔는데, 가 아니었다.", "In summer it wasn't 'he just left.'"),
            new CardDef(23, CardKind.Meet, 15, "가을 — 둘이서", "Autumn — Together", "15챕터 S급", "Chapter 15 with rank S", "가을에 내가 멈췄다.", "In autumn I stopped chasing."),
            new CardDef(24, CardKind.Meet, 20, "겨울 — 둘이서", "Winter — Together", "20챕터 S급", "Chapter 20 with rank S", "6년 걸렸다. 내가 센 게 맞아.", "It took six years. I counted right."),
            new CardDef(25, CardKind.Secret, 0, "라디오 편지", "Radio Letter", "라디오 편지 대성공 3번", "Great success on Radio Letter ×3", "답장이 왔다.", "A reply came."),
            new CardDef(26, CardKind.Secret, 0, "마을 사람", "One of the Village", "평판 50 도달", "Reach Trust 50", "삼촌이 귤 한 상자를 줬다.", "Uncle gave me a box of tangerines."),
            new CardDef(27, CardKind.Secret, 0, "감성 60", "Sense 60", "감성 60 도달", "Reach Sense 60", "우도에 반딧불이 있었다.", "There were fireflies on Udo."),
            new CardDef(28, CardKind.Secret, 0, "재도전", "Second Try", "재도전으로 S급 승급", "Upgrade a chapter to S on retry", "다시 뛰었다.", "I ran it again."),
            new CardDef(29, CardKind.Ending, 0, "엔딩 A — 만난다", "Ending A — We Meet", "전 챕터 S급", "All chapters rank S", "나 간다.", "I'm going."),
            new CardDef(30, CardKind.Ending, 0, "엔딩 B — 못 만난다", "Ending B — We Don't", "엔딩 B 도달", "Reach ending B", "날이 먼저 갔다. 네 탓 아니야.", "The day left first. Not your fault."),
        };

        public static CardDef Get(int id) => Cards[Mathf.Clamp(id, 1, Count) - 1];

        /// 38차: 등급 — 챕터 1~10 N, 챕터 11~20 R, 시크릿 25~28 SR, 만남 21~24·엔딩 29~30 SSR.
        public static CardGrade GradeOf(int id)
        {
            if (id >= 21 && id <= 24) return CardGrade.SSR;
            if (id >= 29) return CardGrade.SSR;
            if (id >= 25) return CardGrade.SR;
            return id <= 10 ? CardGrade.N : CardGrade.R;
        }
    }
}
