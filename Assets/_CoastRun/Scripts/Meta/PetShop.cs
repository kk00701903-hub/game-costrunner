using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 상점: 펫을 사고 장착한다. 소유는 SaveData.ownedPetMask 비트, 장착은 equippedPet.
    /// 52차(사용자): 펫은 **스토리 20주차부터 잠금해제**, 값은 러닝에서 모은 **코인**. 53차: 젤리는 경험치가 됐으니 젤리 대신 **레벨 조건**(LevelReq).
    public static class PetShop
    {
        public const int UnlockWeek = 20;
        public static readonly PetKind[] ForSale = { PetKind.Sparrow, PetKind.BlackPig, PetKind.BikerThug, PetKind.WildGoose };

        public static readonly Dictionary<PetKind, int> Price = new Dictionary<PetKind, int>
        {
            { PetKind.Sparrow, 800 },
            { PetKind.BlackPig, 1500 },   // 14차: 부활 펫 — 코인 쓸 곳
            { PetKind.BikerThug, 2000 },
            { PetKind.WildGoose, 4500 },
        };
        public static readonly Dictionary<PetKind, int> LevelReq = new Dictionary<PetKind, int>
        {
            { PetKind.Sparrow, 3 },
            { PetKind.BlackPig, 6 },
            { PetKind.BikerThug, 9 },
            { PetKind.WildGoose, 12 },
        };

        /// 20주차 전엔 상점이 잠겨 있다(devUnlockAll 이면 열림).
        public static bool Unlocked(SaveData s) => s != null && (s.week >= UnlockWeek || (GameManager.I != null && GameManager.I.Profile != null && GameManager.I.Profile.devUnlockAll));

        public static bool Owns(SaveData s, PetKind k) =>
            k == PetKind.None || (s != null && (s.ownedPetMask & (1 << (int)k)) != 0);

        public static bool CanAfford(SaveData s, PetKind k) =>
            s != null && Price.TryGetValue(k, out int p) && CoinWallet.TotalStatic >= p && Mathf.Max(1, s.level) >= LevelReq[k];

        public static bool TryBuy(SaveData s, PetKind k)
        {
            if (s == null || Owns(s, k) || !Unlocked(s) || !Price.TryGetValue(k, out int price)) return false;
            if (CoinWallet.TotalStatic < price || Mathf.Max(1, s.level) < LevelReq[k]) return false;
            if (!CoinWallet.TrySpendStatic(price)) return false;
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
