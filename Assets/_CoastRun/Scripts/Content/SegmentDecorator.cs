using UnityEngine;

namespace CoastRun
{
    /// Fills each promenade segment with seasonal reusable props.
    public static class SegmentDecorator
    {
        public static void Decorate(Transform segmentRoot, int segmentIndex, SeasonKind season)
        {
            var rng = new System.Random(segmentIndex * 9176 + (int)season * 13);
            float roadHalf = PromenadeSegmentBuilder.RoadHalfWidth;
            float length = PromenadeSegmentBuilder.Length;

            var townPool = PropCatalog.PoolFor(season, true);
            var seaPool = PropCatalog.PoolFor(season, false);

            int townCount = 2 + rng.Next(2);
            for (int i = 0; i < townCount; i++)
            {
                var id = townPool[rng.Next(townPool.Length)];
                float z = 2f + (float)rng.NextDouble() * (length - 4f);
                float x = -(roadHalf + 2.8f + (float)rng.NextDouble() * 3.5f);
                PropCatalog.Spawn(id, segmentRoot, new Vector3(x, 0f, z), season, rng);
            }

            // Keep the sea rail clean — no sidewalk clutter on the drop edge.
            if (rng.NextDouble() < 0.35)
            {
                var id = seaPool[rng.Next(seaPool.Length)];
                float z = 4f + (float)rng.NextDouble() * (length - 8f);
                PropCatalog.Spawn(id, segmentRoot, new Vector3(roadHalf + 1.35f, 0f, z), season, rng);
            }

            // 65차(사용자): 차도 위 계절 상자(가을 낙엽 더미 · 겨울 눈 둑 · 봄 물웅덩이 = 반투명 네모)가 「주인공 옆 네모 오류」로 보였다 → 전부 삭제.
            //   (낙엽·눈·꽃잎은 WeatherFx 파티클과 SnowCaps, 인도 소품으로 이미 표현된다)
        }
    }
}
