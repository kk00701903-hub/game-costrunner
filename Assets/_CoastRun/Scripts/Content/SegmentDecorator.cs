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

            // Road surface accents by weather/season
            if (season == SeasonKind.Autumn && rng.NextDouble() < 0.55)
            {
                PropCatalog.Spawn(PropId.LeafPile, segmentRoot,
                    new Vector3(((float)rng.NextDouble() - 0.5f) * roadHalf, 0f, 8f + (float)rng.NextDouble() * 14f),
                    season, rng);
            }

            if (season == SeasonKind.Winter && rng.NextDouble() < 0.6)
            {
                PropCatalog.Spawn(PropId.SnowBank, segmentRoot,
                    new Vector3(-roadHalf + 0.4f, 0f, 5f + (float)rng.NextDouble() * 18f),
                    season, rng);
            }

            if (season == SeasonKind.Spring && rng.NextDouble() < 0.4)
            {
                PropCatalog.Spawn(PropId.PuddleDecal, segmentRoot,
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 2f, 0f, 10f + (float)rng.NextDouble() * 12f),
                    season, rng);
            }
        }
    }
}
