using System.Collections;
using UnityEngine;

namespace CoastRun.Village
{
    /// 199차: 광산 던전(깊은 갱도 B1~B5) 연결 — 입구(광산 안 구멍)·계단·싸움(행동 버튼)·광맥·보스 보상·쓰러지면 병원.
    public partial class VillageHub
    {
        VillageDungeon _dun; Vector3 _dunHole; bool _hospDun; bool _dunIntroSeen;

        void AddDungeonSpots()
        {
            var go = new GameObject("Dungeon"); go.transform.SetParent(transform, false);
            _dun = go.AddComponent<VillageDungeon>(); _dun.Player = _player; _dun.Locked = () => _busy || _hud.Locked || _interior != null;
            _dun.OnHurt = DungeonHit; _dun.OnToast = m => CoastToast.Show(m); _dun.OnBossDown = DungeonBossDown;
            _dunHole = PickDungeonHole(); BuildDungeonHole(_dunHole);
            _spots.Add(new Spot { id = "dun_enter", title = Loc.T("⬇ 깊은 갱도 (던전) · 내려가기", "⬇ Deep shaft (dungeon) · Go down"), pos = _dunHole, radius = 2.2f, on = () => { if (!_busy) StartCoroutine(DunGo(1)); } });
            _spots.Add(new Spot { id = "dun_down", title = "", pos = new Vector3(9999f, 0f, 9999f), radius = 2.2f, on = () => { if (!_busy && _dun.Floor > 0) StartCoroutine(DunGo(_dun.Floor + 1)); } });
            _spots.Add(new Spot { id = "dun_up", title = "", pos = new Vector3(9999f, 0f, 9999f), radius = 2.0f, on = () => { if (!_busy && _dun.Floor > 0) StartCoroutine(DunGo(_dun.Floor - 1)); } });
        }

        /// 광산 방 안에서 광맥·입구 사다리·수레·레일과 가장 먼 자리(구멍)
        Vector3 PickDungeonHole()
        {
            var C = VillageZones.MineC; Vector3 best = C + new Vector3(-8f, 0f, 8f); float bestD = -1f;
            for (float x = -VillageZones.MineHX + 3f; x <= VillageZones.MineHX - 3f; x += 1f)
                for (float z = -VillageZones.MineHZ + 3f; z <= VillageZones.MineHZ - 3f; z += 1f)
                {
                    var p = C + new Vector3(x, 0f, z); if (Mathf.Abs(x) < 2.2f) continue;
                    float d = Vector3.Distance(p, VillageZones.MineEntry) * 0.6f; d = Mathf.Min(d, Vector3.Distance(p, C + new Vector3(0f, 0f, 5f)));
                    foreach (var n in VillageZones.OreNodes) if (n.t != null) d = Mathf.Min(d, Vector3.Distance(new Vector3(n.t.position.x, 0f, n.t.position.z), p));
                    if (d > bestD) { bestD = d; best = p; }
                }
            return best;
        }
        void BuildDungeonHole(Vector3 at)
        {
            var root = new GameObject("DungeonHole").transform; root.SetParent(VillageZones.Root != null ? VillageZones.Root : transform, false);
            var dark = CoastMaterials.CreateUnlit(new Color(0.03f, 0.02f, 0.03f)); var rock = CoastMaterials.CreateLit(new Color(0.30f, 0.26f, 0.24f), 0.06f); var red = CoastMaterials.CreateUnlit(new Color(1f, 0.45f, 0.35f));
            var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(hole.GetComponent<Collider>()); hole.transform.SetParent(root, false); hole.transform.position = at + Vector3.up * 0.02f; hole.transform.localScale = new Vector3(2.6f, 0.02f, 2.6f); hole.GetComponent<MeshRenderer>().sharedMaterial = dark;
            for (int k = 0; k < 7; k++) { float a = k / 7f * Mathf.PI * 2f; var r = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(r.GetComponent<Collider>()); r.transform.SetParent(root, false); r.transform.position = at + new Vector3(Mathf.Cos(a) * 1.55f, 0.18f, Mathf.Sin(a) * 1.55f); r.transform.localScale = new Vector3(0.7f, 0.4f, 0.7f); r.GetComponent<MeshRenderer>().sharedMaterial = rock; }
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(glow.GetComponent<Collider>()); glow.transform.SetParent(root, false); glow.transform.position = at + new Vector3(1.6f, 1.9f, 0f); glow.transform.localScale = Vector3.one * 0.3f; glow.GetComponent<MeshRenderer>().sharedMaterial = red;
            VillageZones.Sign(root, at + new Vector3(0f, 2.3f, 1.7f), 180f, "⬇ 깊은 갱도 · 보스 주의", "⬇ Deep shaft · Boss", new Color(0.55f, 0.22f, 0.20f), 2.6f, 0.55f);
        }

        void TickDungeon()
        {
            if (_dun == null) return;
            // 병원 등으로 던전 밖에 나오면 층을 치운다
            if (_dun.Active && !_busy && !VillageDungeon.Contains(_player.position)) { _dun.Clear(); UpdateDunSpots(); }
            // 저장된 자리가 갱도 안인데 층이 없으면(다시 켰을 때) 광산 구멍 옆으로
            else if (!_dun.Active && !_busy && VillageDungeon.Contains(_player.position)) Teleport(_dunHole + new Vector3(0f, 0f, -2.4f), 180f);
        }
        public bool InDungeon => _dun != null && _dun.Active && VillageDungeon.Contains(_player.position);

        void UpdateDunSpots()
        {
            var dn = _spots.Find(s => s.id == "dun_down"); var up = _spots.Find(s => s.id == "dun_up");
            bool on = _dun != null && _dun.Active;
            if (dn != null) { dn.pos = on && _dun.Floor < VillageDungeon.BossFloor ? _dun.StairsDown : new Vector3(9999f, 0f, 9999f); dn.title = on && _dun.Floor + 1 >= VillageDungeon.BossFloor ? Loc.T($"⬇ B{_dun.Floor + 1} 보스 방으로 · 내려가기", $"⬇ B{_dun.Floor + 1} Boss room") : Loc.T($"⬇ B{(on ? _dun.Floor + 1 : 1)} 로 내려가기", $"⬇ Down to B{(on ? _dun.Floor + 1 : 1)}"); }
            if (up != null) { up.pos = on ? _dun.StairsUp : new Vector3(9999f, 0f, 9999f); up.title = on && _dun.Floor == 1 ? Loc.T("🪜 광산으로 올라가기", "🪜 Up to the mine") : Loc.T($"🪜 B{(on ? _dun.Floor - 1 : 0)} 로 올라가기", $"🪜 Up to B{(on ? _dun.Floor - 1 : 0)}"); }
        }

        IEnumerator DunGo(int floor)
        {
            _busy = true; yield return FadeScreen(true, 0.35f);
            if (floor <= 0)
            {
                _dun.Clear(); Teleport(_dunHole + new Vector3(0f, 0f, -2.4f), 180f);
            }
            else
            {
                int seed = Save != null ? Save.week * 31 + Save.seed : 1;
                _dun.Build(floor, seed + (int)(Time.time * 10f) % 997);   // 들어갈 때마다 조금씩 다른 굴
                if (floor >= VillageDungeon.BossFloor) _dun.SpawnBoss(DungeonBossKind(), 20 + Mathf.Min(10, Save != null ? Save.dungeonBossKills * 2 : 0));
                Teleport(_dun.StartPos, 0f);
                if (Save != null && floor > Save.dungeonDeepest) { Save.dungeonDeepest = floor; _gm.Persist(); }
            }
            _camYaw = _camYawTarget = _player.eulerAngles.y; SnapCamera(); UpdateDunSpots();
            yield return null; yield return FadeScreen(false, 0.35f); _busy = false;
            if (floor <= 0) { CoastToast.Pop(Loc.T("⛏ 오름 광산", "⛏ Oreum Mine")); yield break; }
            if (floor >= VillageDungeon.BossFloor)
            {
                CoastToast.Pop(Loc.T($"B{floor} · 👑 {BossModel3D.NameKo(_dun.BossKind)}", $"B{floor} · 👑 Boss"));
                _hud.Bubble(Loc.T("광부 할아버지", "Old miner"), Loc.T("저놈이 갱도를 막고 있었구나! 공격을 피하고, 어지러워할 때 세게 쳐!", "That's what blocked the shaft! Dodge, then hit hard while it's dizzy!"));
            }
            else
            {
                CoastToast.Pop(Loc.T($"⛏ 깊은 갱도 B{floor}", $"⛏ Deep shaft B{floor}"));
                if (!_dunIntroSeen) { _dunIntroSeen = true; _hud.Bubble(Loc.T("광부 할아버지", "Old miner"), Loc.T("깊이 갈수록 광맥이 좋아지고 몬스터도 세져. 방망이·곡괭이·도끼로 싸우고, 붉은 등불 쪽 구멍으로 내려가. 맨 밑엔 보스가 있어!", "Deeper = better ore, tougher monsters. Fight with bat/pick/axe; the red lamp marks the way down. A boss waits at the bottom!")); }
            }
        }

        int DungeonBossKind() => Save != null ? Mathf.Abs(Save.week + Save.seed) % 3 : 0;

        /// 행동 버튼 — 던전 안이면 싸움·광맥이 먼저
        bool DungeonAct()
        {
            if (!InDungeon) return false;
            if (_pick) { int oi = _dun.NearOre(_player.position); if (oi >= 0) { StartCoroutine(DunOreCo(oi)); return true; } }
            int dmg = _bat ? 2 + Tier(1) : _axe ? 2 + Tier(2) : _pick ? 1 + Tier(3) : 0;
            if (dmg == 0) { Swing(); CoastToast.Show(Loc.T("잠자리채·낚싯대로는 못 싸운다 — 방망이·곡괭이·도끼를 들자", "Can't fight with a net or rod — use the bat, pick or axe")); return true; }
            Swing();
            var msg = _dun.Swing(dmg, _player.forward);
            if (msg == null) { CoastToast.Show(Loc.T("붕— 헛스윙. 몬스터 가까이에서 휘두르자.", "Whoosh — get closer to a monster.")); return true; }
            if (msg == "") return true;
            if (msg.StartsWith("mob:"))
            {
                int f = Mathf.Max(1, _dun.Floor); int coins = 4 + f * 3; Save.stats.money += coins; string extra = "";
                if (UnityEngine.Random.value < 0.35f) { LifeItems.Add(Save, "mat_stone", 1); extra += Loc.T(" · 돌 +1", " · stone +1"); }
                if (f >= 3 && UnityEngine.Random.value < 0.2f) { LifeItems.Add(Save, "ore_iron", 1); extra += Loc.T(" · 철광석 +1", " · iron +1"); }
                _gm.Persist(); RefreshStatus();
                CoastToast.Show(Loc.T($"💥 물리쳤다! +{coins}G{extra} (남은 몬스터 {_dun.MobsLeft})", $"💥 Defeated! +{coins}G{extra} ({_dun.MobsLeft} left)"));
                return true;
            }
            CoastToast.Show(msg); return true;
        }

        IEnumerator DunOreCo(int idx)
        {
            _busy = true;
            for (int h = 0; h < 2; h++) { Swing(); CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit); yield return new WaitForSeconds(0.35f); }
            int kind = _dun.TakeOre(idx); float r = UnityEngine.Random.value; string id; int cnt = 1;
            switch (kind)
            {
                case 0: id = "ore_iron"; cnt = UnityEngine.Random.Range(1, 3) + (Tier(3) >= 2 ? 1 : 0); break;
                case 1: id = "ore_silver"; cnt = 1 + (UnityEngine.Random.value < 0.3f ? 1 : 0); break;
                case 2: id = "ore_gold"; break;
                case 3: r = Mathf.Min(0.999f, r + 0.1f * Tier(3) + 0.05f * _dun.Floor); id = r < 0.55f ? "gem_amethyst" : r < 0.88f ? "gem_jade" : "gem_ruby"; break;
                default: id = r < 0.3f ? "fossil_ammonite" : r < 0.55f ? "fossil_trilobite" : r < 0.85f ? "fossil_fern" : "fossil_shark"; break;
            }
            LifeItems.Add(Save, id, cnt); MissionTick(VillageMission.Kind.Ore); _gm.Persist(); RefreshStatus();
            var def = LifeItems.Get(id); string nm = def.HasValue ? LifeItems.Name(def.Value) : id;
            CoastToast.Show(Loc.T($"⛏ {nm} ×{cnt}", $"⛏ {nm} ×{cnt}")); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.6f);
            _busy = false;
        }

        void DungeonHit(int dmg, string who)
        {
            if (Save == null || _busy) return;
            Save.stats.stamina = Mathf.Max(0, Save.stats.stamina - dmg);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent(); StartCoroutine(HitFlash());
            if (_motion != null) _motion.Hop();
            if (Save.stats.stamina <= 0) { _hospDun = true; _dun.Clear(); UpdateDunSpots(); StartCoroutine(Hospital()); return; }
            CoastToast.Show(Loc.T($"💢 {who}에게 맞았다! HP −{dmg} (남은 HP {Save.stats.stamina})", $"💢 Hit by {who}! HP −{dmg} (HP {Save.stats.stamina})"));
        }

        void DungeonBossDown(int kind)
        {
            if (Save == null) return;
            bool first = Save.dungeonBossWeek != Save.week; Save.dungeonBossWeek = Save.week; Save.dungeonBossKills++;
            string gem = UnityEngine.Random.value < 0.5f ? "gem_ruby" : "gem_jade";
            if (first) { Save.stats.money += 2000; JellyWallet.Add(40); LifeItems.Add(Save, "ore_gold", 2); LifeItems.Add(Save, gem, 1); }
            else { Save.stats.money += 500; JellyWallet.Add(5); LifeItems.Add(Save, "ore_silver", 2); }
            _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.8f);
            var def = LifeItems.Get(gem); string gn = def.HasValue ? LifeItems.Name(def.Value) : gem;
            CoastToast.Pop(Loc.T($"👑 {BossModel3D.NameKo(kind)} 퇴치!", "👑 Boss defeated!"));
            _hud.Bubble(Loc.T("광부 할아버지", "Old miner"), first
                ? Loc.T($"해냈구나! 이번 주 첫 퇴치 보상 — 2,000G · 젤리 40 · 금광석 2 · {gn} 1. 사다리로 올라가자.", $"You did it! First clear this week — 2,000G · 40 jelly · gold ore ×2 · {gn}. Take the ladder up.")
                : Loc.T("또 이겼네! 500G · 젤리 5 · 은광석 2. 큰 보상은 다음 주에 또 있어.", "Won again! 500G · 5 jelly · silver ore ×2. The big prize resets next week."));
        }

        // ── 개발용 ──
        public void DevDungeon(int floor) { DevLeaveInterior(); if (_dun != null) StartCoroutine(DunGo(floor)); }
        public void DevDungeonLog() { Debug.LogWarning("[Dungeon] " + (_dun != null ? _dun.DevState() : "none") + $" in={InDungeon} hp={(Save != null ? Save.stats.stamina : -1)} hole={_dunHole}"); }
        public void DevDunSwing() { if (_dun == null) return; var t = _dun.DevTarget(); if (!t.HasValue) { Debug.LogWarning("[Dungeon] no target"); return; } var d = t.Value - _player.position; d.y = 0f; var at = t.Value - d.normalized * 1.3f; Teleport(new Vector3(at.x, 0f, at.z), Quaternion.LookRotation(d.normalized).eulerAngles.y); _rod = _pick = _axe = _road = false; _bat = true; ApplyTool(); bool ok = DungeonAct(); Debug.LogWarning("[Dungeon] swing ok=" + ok + " " + _dun.DevState() + " money=" + Save.stats.money); }
        public void DevBossHurt() { if (_dun != null) _dun.DevHurtBoss(18); }
        public void DevBugLog() { Debug.LogWarning("[Bugs] " + (_creatures != null ? _creatures.DevBugLog() : "none")); }
        public void DevManLog() { Debug.LogWarning("[StoryMan] " + (VillageStoryMan.I != null ? VillageStoryMan.I.DevState() : "none")); }
        public void DevManApproach() { if (VillageStoryMan.I != null) VillageStoryMan.I.DevApproach(p => Teleport(p)); }
        public void DevAutoMoveMenu() { AutoMoveMenu(); }
    }
}
