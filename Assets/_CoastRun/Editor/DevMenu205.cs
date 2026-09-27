using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 205차: 재미 요소(동물 돌보기·농사 확장·하루 정산·하트 이벤트·축제 대회) 점검용
    public static class DevMenu205
    {
        static CoastRun.Village.VillageHub H => CoastRun.Village.VillageHub.I;
        [MenuItem("Coast Run/Dev/205 - Day summary")] static void A() { H?.DevDaySummary(); }
        [MenuItem("Coast Run/Dev/205 - Care hens")] static void B() { H?.DevCare(0); }
        [MenuItem("Coast Run/Dev/205 - Care rabbits")] static void C() { H?.DevCare(1); }
        [MenuItem("Coast Run/Dev/205 - Ranch menu")] static void D() { H?.DevCare(2); }
        [MenuItem("Coast Run/Dev/205 - Love +40")] static void E() { H?.DevLove(40); }
        [MenuItem("Coast Run/Dev/205 - Ride")] static void F() { H?.DevRide(); }
        [MenuItem("Coast Run/Dev/205 - Garden menu")] static void G() { H?.DevGarden(); }
        [MenuItem("Coast Run/Dev/205 - Farm log")] static void Hh() { H?.DevFarmLog(); }
        [MenuItem("Coast Run/Dev/205 - Farm probe")] static void I() { H?.DevFarmProbe(); }
        [MenuItem("Coast Run/Dev/205 - Heart event (florist 50)")] static void J() { H?.DevHeart(2, 2); }
        [MenuItem("Coast Run/Dev/205 - Go west garden")] static void O() { H?.DevGoWestGarden(); }
        [MenuItem("Coast Run/Dev/205 - Ripen all")] static void P() { H?.DevRipen(); }
        [MenuItem("Coast Run/Dev/205 - Harvest tile 0")] static void Q() { if (H != null) { H.DevTile = 0; H.DevHarvestHere(); } }
        [MenuItem("Coast Run/Dev/205 - Harvest tile 1")] static void Q1() { if (H != null) { H.DevTile = 1; H.DevHarvestHere(); } }
        [MenuItem("Coast Run/Dev/206 - Go mine mouth")] static void MM() { H?.DevGoMineMouth(); }
        [MenuItem("Coast Run/Dev/205 - Money +10000")] static void N() { H?.DevMoney(); }
        [MenuItem("Coast Run/Dev/205 - Give contest items")] static void M() { H?.DevGiveContestItems(); }
        [MenuItem("Coast Run/Dev/205 - Contest crop fair")] static void K() { H?.DevContest(0); }
        [MenuItem("Coast Run/Dev/205 - Contest fishing")] static void L() { H?.DevContest(1); }
    }
}
