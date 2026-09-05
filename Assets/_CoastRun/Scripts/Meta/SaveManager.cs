using System;
using System.IO;
using UnityEngine;

namespace CoastRun
{
    /// JSON 세이브 — persistentDataPath/save_0.json (회차) + profile.json (해금).
    /// 예전 PlayerPrefs 키(코인·펫)는 첫 로드 때 한 번 흡수한다.
    public class SaveManager : MonoBehaviour
    {
        public const string SaveFile = "save_0.json";
        public const string ProfileFile = "profile.json";

        private MetaProfile _profile;
        private System.Random _rng;
        private int _rngSeed;
        private int _rngCount;

        public MetaProfile Profile => _profile ??= LoadProfile();

        /// 회차 시드 기반 난수 — 세이브에 seed/rollCount를 기록해 재현 가능.
        public System.Random Rng => _rng ??= new System.Random(_rngSeed);

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);
        public static string ProfilePath => Path.Combine(Application.persistentDataPath, ProfileFile);

        public bool HasSave => File.Exists(SavePath);

        public SaveData CreateNew()
        {
            var s = new SaveData
            {
                seed = Environment.TickCount ^ (int)DateTime.Now.Ticks,
                playthrough = Mathf.Max(1, Profile.endingsSeen + 1),
            };
            // 이전 빌드에서 모은 코인은 새 회차의 초기 자금으로 한 번 흡수.
            int legacyCoins = PlayerPrefs.GetInt(CoinWallet.PrefsKey, 0);
            if (legacyCoins > 0)
                s.stats.money += Mathf.Min(legacyCoins, 500);
            BindRng(s);
            return s;
        }

        public SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return null;
                var s = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (s == null || s.chapters == null || s.chapters.Length != Timeline.Chapters)
                    return null;
                BindRng(s);
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] load failed: " + e.Message);
                return null;
            }
        }

        public void Write(SaveData s)
        {
            if (s == null) return;
            try
            {
                s.rollCount = _rngCount;
                File.WriteAllText(SavePath, JsonUtility.ToJson(s, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] write failed: " + e.Message);
            }
        }

        public void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning("[Save] delete failed: " + e.Message); }
        }

        public void WriteProfile(MetaProfile p)
        {
            _profile = p;
            try { File.WriteAllText(ProfilePath, JsonUtility.ToJson(p, true)); }
            catch (Exception e) { Debug.LogWarning("[Save] profile write failed: " + e.Message); }
        }

        private static MetaProfile LoadProfile()
        {
            try
            {
                if (File.Exists(ProfilePath))
                    return JsonUtility.FromJson<MetaProfile>(File.ReadAllText(ProfilePath)) ?? new MetaProfile();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] profile load failed: " + e.Message);
            }
            var p = new MetaProfile();
            // 예전 빌드에서 캠페인을 이미 깼다면 스케이트보드도 열어 둔다.
            if (PlayerPrefs.GetInt(ProgressionManager.ClearedKey, 0) == 1)
            {
                p.endingsSeen = 1;
                p.skateboardUnlocked = true;
            }
            return p;
        }

        private void BindRng(SaveData s)
        {
            _rngSeed = s.seed;
            _rng = new System.Random(s.seed);
            _rngCount = 0;
            for (int i = 0; i < s.rollCount; i++)
                _rng.Next();
            _rngCount = s.rollCount;
        }

        public double NextDouble()
        {
            _rngCount++;
            return Rng.NextDouble();
        }

        public int NextInt(int maxExclusive)
        {
            _rngCount++;
            return Rng.Next(maxExclusive);
        }
    }
}
