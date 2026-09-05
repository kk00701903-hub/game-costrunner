using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 상점: 돈으로 펫을 사고 장착한다. 소유는 SaveData.ownedPetMask 비트, 장착은 equippedPet.
    public static class PetShop
    {
        public static readonly PetKind[] ForSale = { PetKind.Sparrow, PetKind.BikerThug, PetKind.WildGoose };

        public static readonly Dictionary<PetKind, int> Price = new Dictionary<PetKind, int>
        {
            { PetKind.Sparrow, 800 },
            { PetKind.BikerThug, 2000 },
            { PetKind.WildGoose, 4500 },
        };

        public static bool Owns(SaveData s, PetKind k) =>
            k == PetKind.None || (s != null && (s.ownedPetMask & (1 << (int)k)) != 0);

        public static bool CanAfford(SaveData s, PetKind k) =>
            s != null && Price.TryGetValue(k, out int p) && s.stats.money >= p;

        public static bool TryBuy(SaveData s, PetKind k)
        {
            if (s == null || Owns(s, k) || !Price.TryGetValue(k, out int price) || s.stats.money < price)
                return false;
            s.stats.money -= price;
            s.ownedPetMask |= 1 << (int)k;
            if (s.equippedPet == PetKind.None)
                s.equippedPet = k;
            return true;
        }

        public static bool Equip(SaveData s, PetKind k)
        {
            if (s == null || !Owns(s, k)) return false;
            s.equippedPet = k;
            return true;
        }
    }
}
