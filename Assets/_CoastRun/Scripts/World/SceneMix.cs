using UnityEngine;

namespace CoastRun
{
    /// 35차: 챕터별 장면 배합 — 30 m 타일마다 왼쪽/오른쪽을 무엇으로 채울지 확률로 고른다(2타일씩 같은 장면 유지).
    /// 사진 레퍼런스: 삼나무 숲길, 해안도로(무지개 방호벽), 해변, 풍력 발전기 해안, 현무암 바위밭, 오름 초원, 유채/메밀/억새밭, 마을·시장.
    public enum LeftKind { Town, Village, Forest, Field, Rock }
    public enum RightKind { Sea, Rainbow, Beach, WindSea, Hill, Forest, Field, Rock, Town }
    public enum FieldKind { Grass, Canola, Buckwheat, SilverGrass, Orchard }
    public enum ForestKind { Cedar, Pine, Camellia, Broadleaf }

    public struct SceneProfile
    {
        public (LeftKind kind, int w)[] left;
        public (RightKind kind, int w)[] right;
        public FieldKind field;
        public ForestKind forest;
        public int runLength;   // 같은 장면을 몇 타일 유지하나
    }

    public static class SceneMix
    {
        private static SceneProfile P(FieldKind f, ForestKind fo, (LeftKind, int)[] l, (RightKind, int)[] r, int run = 2)
            => new SceneProfile { left = l, right = r, field = f, forest = fo, runLength = run };

        // 챕터 1~20 (ChapterLocation 표와 같은 순서)
        private static readonly SceneProfile[] Table =
        {
            /* 1 애월 무지개 해안도로 */ P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Town, 55), (LeftKind.Village, 30), (LeftKind.Field, 15) }, new[] { (RightKind.Rainbow, 60), (RightKind.Sea, 40) }),
            /* 2 이호테우 해변 */       P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Town, 45), (LeftKind.Village, 30), (LeftKind.Field, 25) }, new[] { (RightKind.Beach, 55), (RightKind.Sea, 45) }),
            /* 3 하늘네 마을 */         P(FieldKind.Orchard, ForestKind.Broadleaf, new[] { (LeftKind.Village, 65), (LeftKind.Town, 20), (LeftKind.Field, 15) }, new[] { (RightKind.Field, 35), (RightKind.Hill, 30), (RightKind.Sea, 35) }),
            /* 4 칠머리당 마을 */       P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Village, 55), (LeftKind.Town, 30), (LeftKind.Rock, 15) }, new[] { (RightKind.Sea, 50), (RightKind.Field, 30), (RightKind.Town, 20) }),
            /* 5 섭지코지 유채 */       P(FieldKind.Canola, ForestKind.Pine, new[] { (LeftKind.Field, 70), (LeftKind.Village, 30) }, new[] { (RightKind.Field, 50), (RightKind.Sea, 35), (RightKind.Hill, 15) }, 3),
            /* 6 이호테우 밤 축제 */    P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Town, 75), (LeftKind.Village, 25) }, new[] { (RightKind.Beach, 45), (RightKind.Sea, 40), (RightKind.Town, 15) }),
            /* 7 성산 해녀 */           P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Rock, 40), (LeftKind.Village, 40), (LeftKind.Town, 20) }, new[] { (RightKind.Sea, 60), (RightKind.Beach, 25), (RightKind.Rock, 15) }),
            /* 8 동문시장 */            P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Town, 100) }, new[] { (RightKind.Town, 70), (RightKind.Sea, 30) }, 3),
            /* 9 우도 원담 */           P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Field, 45), (LeftKind.Village, 40), (LeftKind.Rock, 15) }, new[] { (RightKind.Sea, 55), (RightKind.Rock, 30), (RightKind.Beach, 15) }),
            /* 10 곶자왈 숲길 */        P(FieldKind.Grass, ForestKind.Cedar, new[] { (LeftKind.Forest, 85), (LeftKind.Rock, 15) }, new[] { (RightKind.Forest, 80), (RightKind.Rock, 20) }, 3),
            /* 11 성읍 귤밭 */          P(FieldKind.Orchard, ForestKind.Broadleaf, new[] { (LeftKind.Village, 45), (LeftKind.Field, 55) }, new[] { (RightKind.Field, 65), (RightKind.Hill, 35) }),
            /* 12 다랑쉬오름 */         P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Field, 60), (LeftKind.Forest, 40) }, new[] { (RightKind.Hill, 80), (RightKind.Field, 20) }, 3),
            /* 13 중산간 메밀밭 */      P(FieldKind.Buckwheat, ForestKind.Pine, new[] { (LeftKind.Field, 75), (LeftKind.Village, 25) }, new[] { (RightKind.Field, 60), (RightKind.Hill, 40) }, 3),
            /* 14 만장굴 */             P(FieldKind.Grass, ForestKind.Cedar, new[] { (LeftKind.Rock, 65), (LeftKind.Forest, 35) }, new[] { (RightKind.Rock, 50), (RightKind.Forest, 50) }),
            /* 15 송악산 억새 */        P(FieldKind.SilverGrass, ForestKind.Pine, new[] { (LeftKind.Field, 70), (LeftKind.Rock, 30) }, new[] { (RightKind.Sea, 50), (RightKind.Hill, 35), (RightKind.Field, 15) }),
            /* 16 본향당 첫눈 마을 */   P(FieldKind.Grass, ForestKind.Broadleaf, new[] { (LeftKind.Village, 75), (LeftKind.Forest, 25) }, new[] { (RightKind.Forest, 45), (RightKind.Field, 40), (RightKind.Sea, 15) }),
            /* 17 방사탑 */             P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Field, 50), (LeftKind.Village, 35), (LeftKind.Rock, 15) }, new[] { (RightKind.Hill, 55), (RightKind.Sea, 45) }),
            /* 18 동백 숲 */            P(FieldKind.Grass, ForestKind.Camellia, new[] { (LeftKind.Forest, 100) }, new[] { (RightKind.Forest, 70), (RightKind.Field, 30) }, 3),
            /* 19 산방산 해안 */        P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Town, 40), (LeftKind.Rock, 30), (LeftKind.Village, 30) }, new[] { (RightKind.Sea, 55), (RightKind.Beach, 30), (RightKind.WindSea, 15) }),
            /* 20 오름 정상·송전탑 */   P(FieldKind.Grass, ForestKind.Pine, new[] { (LeftKind.Field, 60), (LeftKind.Rock, 40) }, new[] { (RightKind.Hill, 55), (RightKind.WindSea, 30), (RightKind.Sea, 15) }),
        };

        // 풍력 발전기 해안은 여러 챕터에 소량 섞는다(사진 레퍼런스의 대표 실루엣)
        public static SceneProfile Get(int chapter) => Table[Mathf.Clamp(chapter, 1, Table.Length) - 1];

        /// 타일 → 장면. runLength 타일씩 같은 결과. 같은 장면이 3번 연속이면 다음은 다른 것으로(지루함 방지).
        public static void Pick(int chapter, int segmentIndex, out LeftKind left, out RightKind right)
        {
            var p = Get(chapter);
            int run = Mathf.Max(1, p.runLength);
            int block = segmentIndex / run;
            left = Weighted(p.left, new System.Random(chapter * 7919 + block * 131 + 1));
            right = Weighted(p.right, new System.Random(chapter * 7919 + block * 131 + 2));
            // 풍력 발전기: 바다가 있는 챕터는 8블록마다 한 번 WindSea (사진의 대표 풍경)
            if (right == RightKind.Sea && block % 8 == 5) right = RightKind.WindSea;
            // 시장(8장)은 오른쪽 상가와 왼쪽 상가가 동시에 오게
            if (chapter == 8 && block % 3 == 1) right = RightKind.Town;
        }

        private static T Weighted<T>((T kind, int w)[] table, System.Random rng)
        {
            int total = 0; foreach (var e in table) total += e.w;
            int r = rng.Next(total);
            foreach (var e in table) { r -= e.w; if (r < 0) return e.kind; }
            return table[table.Length - 1].kind;
        }

        public static string LeftName(LeftKind k) => k switch { LeftKind.Town => "상가", LeftKind.Village => "마을", LeftKind.Forest => "숲", LeftKind.Field => "들판", _ => "바위" };
        public static string RightName(RightKind k) => k switch { RightKind.Sea => "바다", RightKind.Rainbow => "무지개 방호벽", RightKind.Beach => "해변", RightKind.WindSea => "풍력 해안", RightKind.Hill => "오름", RightKind.Forest => "숲", RightKind.Field => "들판", RightKind.Rock => "현무암", _ => "상가" };
    }
}
