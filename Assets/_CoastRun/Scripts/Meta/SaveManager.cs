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
            // 6차 NG+: 지난 회차 최종 스탯의 20%를 물려받는다(돈은 10%).
            if (Profile.hasLastFinal && Profile.lastFinalStats != null && s.playthrough >= 2)
            {
                var f = Profile.lastFinalStats;
                s.stats.stamina += f.stamina / 5; s.stats.agility += f.agility / 5; s.stats.charm += f.charm / 5;
                s.stats.sense += f.sense / 5; s.stats.trust += f.trust / 5; s.stats.money += f.money / 10;
                s.stats.Clamp();
            }
            // 이전 빌드에서 모은 코인은 새 회차의 초기 자금으로 한 번 흡수.
            int legacyCoins = PlayerPrefs.GetInt(CoinWallet.PrefsKey, 0);
            if (legacyCoins > 0)
                s.stats.money += Mathf.Min(legacyCoins, 500);
            BindRng(s);
            return s;
        }

        // 24차-9(점검 2-4): 손상·형식 변경 시 확인 없이 새 회차로 덮어쓰던 것을 막는다.
        //  · 쓰기: 임시파일 → 기존 파일을 .bak 으로 → 교체 (쓰는 도중 크래시가 나도 이전 세이브가 남는다)
        //  · 읽기: 본 파일 실패 → .bak 시도. 손상 파일은 .corrupt.json 으로 옮겨 보존
        //  · 버전: version 필드로 마이그레이션(챕터 수 변경은 배열 리사이즈)
        public const int CurrentVersion = 2;
        public static string BackupPath => SavePath + ".bak";
        public static string CorruptPath => Path.Combine(Application.persistentDataPath, "save_0.corrupt.json");

        /// 마지막 Load 가 손상 파일을 만나 백업/새 회차로 대체됐는지 — 타이틀 안내용.
        public bool LastLoadRecovered { get; private set; }

        public SaveData Load()
        {
            LastLoadRecovered = false;
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                return null;

            var s = TryRead(SavePath, out bool mainCorrupt);
            if (s == null)
            {
                if (mainCorrupt)
                {
                    try { if (File.Exists(CorruptPath)) File.Delete(CorruptPath); File.Move(SavePath, CorruptPath); }
                    catch (Exception e) { Debug.LogWarning("[Save] corrupt file keep failed: " + e.Message); }
                    Debug.LogWarning("[Save] save_0.json 손상 → save_0.corrupt.json 으로 보존, .bak 시도");
                }
                s = TryRead(BackupPath, out _);
                if (s != null) LastLoadRecovered = true;
            }
            if (s == null)
                return null;
            Migrate(s);
            BindRng(s);
            return s;
        }

        private static SaveData TryRead(string path, out bool corrupt)
        {
            corrupt = false;
            if (!File.Exists(path)) return null;
            try
            {
                var txt = File.ReadAllText(path);
                var s = JsonUtility.FromJson<SaveData>(txt);
                if (s == null || s.chapters == null || s.stats == null) { corrupt = true; return null; }
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] load failed (" + Path.GetFileName(path) + "): " + e.Message);
                corrupt = true;
                return null;
            }
        }

        private static void Migrate(SaveData s)
        {
            if (s.chapters.Length != Timeline.Chapters)
            {
                var old = s.chapters;
                var n = new ChapterRecord[Timeline.Chapters];
                for (int i = 0; i < n.Length; i++)
                    n[i] = i < old.Length && old[i] != null ? old[i] : new ChapterRecord { chapter = i + 1 };
                s.chapters = n;
                Debug.Log($"[Save] chapters {old.Length}→{n.Length} 리사이즈");
            }
            if (s.queuedSchedule == null || s.queuedSchedule.Length < Timeline.PhasesPerWeek)
                s.queuedSchedule = new string[Timeline.PhasesPerWeek];
            if (s.affinity == null || s.affinity.Length < 4) s.affinity = new int[4];
            if (s.chapter < 1) s.chapter = 1;
            if (s.week < 1) s.week = 1;
            // version 1 → 2 등 필드 추가는 JsonUtility 기본값으로 채워지므로 도장만 찍는다.
            s.version = CurrentVersion;
        }

        public void Write(SaveData s)
        {
            if (s == null) return;
            try
            {
                s.rollCount = _rngCount;
                s.version = CurrentVersion;
                WriteAtomic(SavePath, JsonUtility.ToJson(s, true), keepBackup: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] write failed: " + e.Message);
            }
        }

        private static void WriteAtomic(string path, string text, bool keepBackup)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(path))
            {
                if (keepBackup)
                {
                    string bak = path + ".bak";
                    try { File.Replace(tmp, path, bak); return; }
                    catch (PlatformNotSupportedException) { }
                    catch (IOException) { }
                    // File.Replace 가 안 되는 플랫폼: 수동 회전
                    try { if (File.Exists(bak)) File.Delete(bak); File.Move(path, bak); } catch { }
                }
                else File.Delete(path);
            }
            File.Move(tmp, path);
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);   // 24차-9: 새 회차 뒤 옛 백업이 되살아나지 않게
            }
            catch (Exception e) { Debug.LogWarning("[Save] delete failed: " + e.Message); }
        }

        public void WriteProfile(MetaProfile p)
        {
            _profile = p;
            try { WriteAtomic(ProfilePath, JsonUtility.ToJson(p, true), keepBackup: true); }
            catch (Exception e) { Debug.LogWarning("[Save] profile write failed: " + e.Message); }
        }

        private static MetaProfile LoadProfile()
        {
            try
            {
                foreach (var path in new[] { ProfilePath, ProfilePath + ".bak" })   // 24차-9: 본 파일 손상 시 .bak
                {
                    if (!File.Exists(path)) continue;
                    MetaProfile loaded = null;
                    try { loaded = JsonUtility.FromJson<MetaProfile>(File.ReadAllText(path)); }
                    catch (Exception e) { Debug.LogWarning("[Save] profile parse failed (" + Path.GetFileName(path) + "): " + e.Message); }
                    if (loaded == null) continue;
                    loaded.EnsureArrays();
                    return loaded;
                }
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
