using UnityEngine;

namespace CoastRun
{
    /// Target ~3 hours first clear (ride + upgrade grind).
    public static class ContentPace
    {
        /// Path metres to 송전탑. ~9 m/s effective ≈ 100 min continuous ride.
        public const float TowerDistanceMetres = 54000f;

        /// MaxSpeed level gate — forces multi-run grind into the 3h envelope.
        public const int TowerRequiredMaxSpeedLevel = 22;

        /// Soft D-Day / sunset pressure window (3 hours).
        public const float DDaySeconds = 10800f;

        // Season bands removed — lightingT is monotonic (one afternoon, never loops).
        [System.Obsolete("Season cycle removed. Use StageManager lightingT.")]
        public const float SeasonBandMetres = 13500f;
        public const float WeatherRollMetres = 900f;
    }

    public enum SeasonKind
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    public enum WeatherKind
    {
        Clear = 0,
        Cloudy = 1,
        Rain = 2,
        Snow = 3,
        Mist = 4
    }

    public enum PropId
    {
        Bench,
        TrashCan,
        BikeRack,
        Planter,
        StreetLamp,
        CafeUmbrella,
        Signboard,
        VendingMachine,
        FireHydrant,
        Mailbox,
        Barrier,
        Cone,
        Barrel,
        Crate,
        SurfboardRack,
        IceCreamCart,
        CherryTree,
        Palm,
        Maple,
        Pine,
        Snowman,
        Pumpkin,
        FlowerBox,
        Sandbag,
        Buoy,
        Lifebuoy,
        FishingRodRack,
        NewspaperStand,
        Scooter,
        DogWalkerNpc,
        TouristNpc,
        KidNpc,
        CoupleNpc,
        ShopAwningSpring,
        ShopAwningSummer,
        ShopAwningAutumn,
        ShopAwningWinter,
        PuddleDecal,
        LeafPile,
        SnowBank,
        FestivalLantern,
        WindChime,
        BirdFlockMarker,
        PowerBox,
        Manhole,
        CrosswalkSign,
        TrafficLight,
        BusStop,
        TaxiStand,
        Fountain,
        StatueSmall
    }

    public enum ObstacleId
    {
        TrafficCone,
        Barrier,
        CrateStack,
        WetFloorSign,
        SnowDrift,
        LeafDrift,
        PuddleSlow,
        TouristCluster,
        DeliveryBox,
        BikeFallen,
        OverheadBar,
        Clothesline
    }
}
