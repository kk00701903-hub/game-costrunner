using System;
using System.Collections.Generic;
using UnityEngine;

/// Three catalogues. Tags are the real collection — 347 names, last is Doha.
public class Codex : MonoBehaviour
{
    public static Codex Instance { get; private set; }

    public const int LostItemCount = 120;
    public const int TagCount = 347;
    public const int SkinCount = 48;
    /// 0-based index of A-0347 서도하 — fills only at the ending.
    public const int PlayerTagId = 346;

    public event Action OnChanged;

    private SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ServiceLocator.Register(this);
        EnsureBrotherListed();
    }

    private void EnsureBrotherListed()
    {
        // A-0348 is already on the list. Flag only — not one of the 347 collectibles.
        if (Save == null)
            return;
        if (!Save.flags.Contains("tag_a0348_listed"))
        {
            Save.flags.Add("tag_a0348_listed");
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.MarkDirty();
        }
    }

    public int LostItemsFound => Save != null ? Save.lostItemIds.Count : 0;
    public int TagsFound => Save != null ? Save.collectedTagIds.Count : 0;
    public int SkinsUnlocked => Save != null ? Save.unlockedSkins.Count : 0;

    public float TagCompletion => TagsFound / (float)TagCount;

    public bool HasTag(int id)
    {
        return Save != null && Save.collectedTagIds.Contains(id);
    }

    public bool RegisterTag(int id)
    {
        if (Save == null || id < 0 || id >= TagCount)
            return false;
        if (Save.collectedTagIds.Contains(id))
            return false;

        Save.collectedTagIds.Add(id);
        Dirty();
        return true;
    }

    /// Runtime pickups map to a deterministic id from the run seed + pick index.
    public int RegisterRandomTag(int runSeed, int pickIndex)
    {
        int id = PositiveMod(runSeed * 31 + pickIndex * 17, TagCount);
        // Never auto-grant the player's own tag — that fills at the ending.
        if (id == PlayerTagId)
            id = (id + 1) % TagCount;
        RegisterTag(id);
        return id;
    }

    public bool RegisterLostItem(int id)
    {
        if (Save == null || id < 0 || id >= LostItemCount)
            return false;
        if (Save.lostItemIds.Contains(id))
            return false;

        Save.lostItemIds.Add(id);
        if (Wallet.Instance != null)
            Wallet.Instance.AddCoins(50, false);
        Dirty();
        return true;
    }

    public bool UnlockSkin(string skinId)
    {
        if (Save == null || string.IsNullOrEmpty(skinId))
            return false;
        if (Save.unlockedSkins.Contains(skinId))
            return false;

        Save.unlockedSkins.Add(skinId);
        Dirty();
        return true;
    }

    public bool UnlockPlayerTag()
    {
        return RegisterTag(PlayerTagId);
    }

    public static string TagLabel(int id)
    {
        if (id == PlayerTagId)
            return "A-0347 서도하";
        if (id < 0 || id >= TagCount)
            return "A-????";

        // Procedural stand-in until the 347-name list is authored.
        int num = id + 1;
        return "A-" + num.ToString("D4") + " " + GeneratedName(id);
    }

    private static string GeneratedName(int id)
    {
        string[] family =
        {
            "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임",
            "한", "오", "서", "신", "권", "황", "안", "송", "홍", "유"
        };
        string[] given =
        {
            "서연", "도윤", "하준", "지호", "수아", "예준", "민서", "주원", "하은", "시우",
            "지유", "준서", "다은", "건우", "소율", "현우", "예린", "우진", "채원", "선우"
        };
        return family[id % family.Length] + given[(id * 7) % given.Length];
    }

    private static int PositiveMod(int value, int mod)
    {
        int r = value % mod;
        return r < 0 ? r + mod : r;
    }

    private void Dirty()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();
        OnChanged?.Invoke();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}
