using System;
using UnityEngine;

namespace CoastRun.Village
{
    /// 160차(사용자: 「하루에 미션 포함 4개까지 하고 그걸 1주로」):
    /// 하루의 네 번째 활동 = 일일 미션. 아침에 한 개가 정해지고(주차 시드), 마을에서 목표를 채우면 활동 하나로 세고 보상을 준다.
    public static class VillageMission
    {
        public enum Kind { Bug = 0, Chop = 1, Mine = 2, Farm = 3, Fish = 4, Pick = 5, Talk = 6 }
        public const int Count = 7;

        /// 주차마다 결정적으로 하나 뽑는다(같은 날 다시 들어와도 같은 미션).
        public static Kind Pick(SaveData s)
        {
            int seed = (s != null ? s.week * 977 + s.seed * 31 + s.playthrough * 7 : 0);
            return (Kind)(Mathf.Abs(seed) % Count);
        }

        public static int Goal(Kind k) => k == Kind.Bug ? 2 : k == Kind.Pick ? 3 : k == Kind.Talk ? 2 : 1;

        public static string Title(Kind k)
        {
            switch (k)
            {
                case Kind.Bug: return Loc.T("벌레 2마리 잡기", "Catch 2 bugs");
                case Kind.Chop: return Loc.T("도끼로 나무 패기", "Chop a tree");
                case Kind.Mine: return Loc.T("곡괭이로 돌 캐기", "Mine a rock");
                case Kind.Farm: return Loc.T("텃밭 돌보기", "Tend the garden");
                case Kind.Fish: return Loc.T("바닷가에서 낚시하기", "Go fishing");
                case Kind.Pick: return Loc.T("바닥에서 3개 줍기", "Pick up 3 things");
                default: return Loc.T("마을 사람 2명과 이야기", "Talk to 2 villagers");
            }
        }
        public static string Hint(Kind k)
        {
            switch (k)
            {
                case Kind.Bug: return Loc.T("잠자리채를 들고 언덕 풀밭으로", "Take the net to the hill");
                case Kind.Chop: return Loc.T("도구에서 도끼를 고르고 나무 옆에서", "Pick the axe, stand by a tree");
                case Kind.Mine: return Loc.T("도구에서 곡괭이를 고르고 언덕 바위로", "Pick the pickaxe, hill rocks");
                case Kind.Farm: return Loc.T("텃밭 칸에서 씨·물·수확 아무거나", "Any plot: seed, water or harvest");
                case Kind.Fish: return Loc.T("바닷가 모래밭에서 낚시", "Beach sand, cast a line");
                case Kind.Pick: return Loc.T("나뭇가지·조개·버섯 아무거나", "Branches, shells, mushrooms");
                default: return Loc.T("마을 골목의 사람들에게 말 걸기", "Villagers in the lane");
            }
        }
        /// 보상: 돈 + 별조각(활동 하나 값어치)
        public static int Money(Kind k) => k == Kind.Chop || k == Kind.Mine ? 45 : k == Kind.Fish ? 40 : 35;
        public const int Shards = 2;
    }
}
