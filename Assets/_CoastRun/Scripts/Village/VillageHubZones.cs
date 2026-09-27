using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 195차(사용자): 버스로 시내·중문관광단지 · 광산 · 과수원/양봉/축사 · 옷 · 청음샵(OST) · 박물관 · 도감/호감도 · 계절 이벤트.
    public partial class VillageHub
    {
        public const int FareCity = 30, FareTour = 60;
        VillageZones.Zone _zoneNow = VillageZones.Zone.None; bool _zonesInit; AudioSource _recSrc; int _recPlaying;

        // ── 스팟 ─────────────────────────────────────────────────────────
        void AddZoneSpots()
        {
            VillageDex.Ensure(Save);
            var C = VillageZones.CityC; var T = VillageZones.TourC;
            _spots.Add(new Spot { id = "citybus", title = Loc.T("🚌 시내 정류장 · 버스 타기", "🚌 City stop · Take the bus"), pos = VillageZones.CityStop, radius = 3f, on = () => BusMenu(VillageZones.Zone.City) });
            _spots.Add(new Spot { id = "tourbus", title = Loc.T("🚌 중몬 정류장 · 버스 타기", "🚌 Jungmon stop · Take the bus"), pos = VillageZones.TourStop, radius = 3f, on = () => BusMenu(VillageZones.Zone.Tour) });
            _spots.Add(new Spot { id = "mine", title = Loc.T("⛏ 오름 광산 · 들어가기", "⛏ Oreum Mine · Enter"), pos = VillageWorld.Ground(VillageZones.MineMouth.x, VillageZones.MineMouth.y - 1.6f), radius = 2.8f, on = EnterMine });
            _spots.Add(new Spot { id = "mineexit", title = Loc.T("🪜 사다리 · 마을로 나가기", "🪜 Ladder · Back to village"), pos = VillageZones.MineEntry, radius = 2.4f, on = ExitMine });
            string[] tk = { "🏖 색동해변 · 산책하기 (스트레스 −15)", "📸 주상절벽대 · 전망 보고 사진 찍기 (스트레스 −10)", "💧 천재연 폭포 · 구경하기 (스트레스 −10)", "🌺 여미니 식물원 · 관람하기 (스트레스 −8)", "🪑 호텔 정원 · 벤치에서 쉬기 (스트레스 −6 · HP +20)" };
            AddActivitySpots();   // 196차: 중문 액티비티
            AddDungeonSpots();   // 199차: 광산 던전
            for (int i = 0; i < 5; i++) { int k = i; _spots.Add(new Spot { id = "tour" + i, title = Loc.T(tk[i], tk[i]), pos = VillageZones.TourSpots[i], radius = 3.2f, on = () => TourAct(k) }); }
            // 동쪽 새 동네: 과수원 · 벌통 · 축사
            _spots.Add(new Spot { id = "orchard", title = Loc.T("🍊 돌담 귤 과수원", "🍊 Stone-wall orchard"), pos = VillageWorld.Ground(40f, 20.2f), radius = 2.8f, on = OrchardMenu });
            _spots.Add(new Spot { id = "hive", title = Loc.T("🐝 유채꽃 벌통", "🐝 Canola beehives"), pos = VillageWorld.Ground(VillageEast.HiveX - 1.4f, 32f), radius = 3.2f, on = HiveMenu });
            _spots.Add(new Spot { id = "barn", title = Loc.T("🐄 축사 · 소·흑돼지", "🐄 Barn · cows & pigs"), pos = VillageWorld.Ground(VillageEast.BankX, VillageEast.BankZ - 4.2f), radius = 3f, on = BarnMenu });
            if (VillageSeason.Booth != null) { var ev = VillageSeason.Now(Save); if (ev != null) _spots.Add(new Spot { id = "event", title = Loc.T($"🎪 {ev.boothKo}", $"🎪 {ev.en}"), pos = VillageSeason.Booth.position + new Vector3(0f, 0f, -1.6f), radius = 2.8f, on = EventBoothMenu }); }
        }

        void TickZones()
        {
            if (!_zonesInit && Save != null && _player != null)
            {
                _zonesInit = true; VillageDex.Ensure(Save); UpdateAnimals(); VillageZones.RefreshOre(Save); RefreshBarn(); RefreshHives(); ApplyOutfit();
                StartCoroutine(SeasonIntro());
            }
            if (_player == null) return;
            var z = VillageZones.At(_player.position);
            if (z != _zoneNow) { _zoneNow = z; if (_map != null) _map.SetHidden(z != VillageZones.Zone.None); }
            { bool hideMap = z != VillageZones.Zone.None || _interior != null; if (hideMap != _mapHid211) { _mapHid211 = hideMap; if (_map != null) _map.SetHidden(hideMap); } }   // 211차: 집·가게 안에서도 마을 미니맵 숨김
            // 조개 줍기(색달해변) — 가까이 가면 줍는다
            if (z == VillageZones.Zone.Tour && !_busy)
                for (int i = VillageZones.Shells.Count - 1; i >= 0; i--)
                {
                    var sh = VillageZones.Shells[i]; if (sh == null || !sh.gameObject.activeSelf) continue;
                    if (Vector3.Distance(new Vector3(sh.position.x, 0f, sh.position.z), new Vector3(_player.position.x, 0f, _player.position.z)) < 1.1f)
                    {
                        sh.gameObject.SetActive(false); LifeItems.Add(Save, "gath_shell", 1); MissionTick(VillageMission.Kind.TourShell);
                        VillagePang.Burst(sh.position + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.8f), Color.white, 0.8f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
                        CoastToast.Show(Loc.T("🐚 색동해변 조개껍데기를 주웠다!", "🐚 Picked up a Saekdong shell!")); _gm.Persist();
                    }
                }
            // 청음샵 밖으로 나가면 음악 끄기
            if (_recSrc != null && _recSrc.isPlaying && _interiorKind != "records") StopRecord();
        }

        IEnumerator SeasonIntro()
        {
            yield return new WaitForSeconds(2.5f);
            var ev = VillageSeason.Now(Save); if (ev == null || Save == null || Save.seasonEventSeen == ev.from) yield break;
            Save.seasonEventSeen = ev.from; _gm.Persist();
            CoastToast.Pop(Loc.T($"🎉 {ev.ko} 시작! ({ev.from}~{ev.to}주차)", $"🎉 {ev.en} begins!"));
            for (int i = 0; i < 6; i++) { VillagePang.Burst(_player.position + new Vector3(UnityEngine.Random.Range(-4f, 4f), 5f + UnityEngine.Random.Range(0f, 2f), UnityEngine.Random.Range(2f, 6f)), ev.col, Color.white, 2.2f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.35f); yield return new WaitForSeconds(0.35f); }
            _hud.Bubble(Loc.T("꼬마", "Kid"), Loc.T($"누나! {ev.ko} 부스가 열렸대. 한정 물건도 있대!", $"Sis! The {ev.en} booth is open — limited goods!"));
        }

        // ── 버스 ─────────────────────────────────────────────────────────
        void BusMenu(VillageZones.Zone from)
        {
            var list = new List<(string, Color, Action)>();
            if (from != VillageZones.Zone.None) list.Add((Loc.T($"🏡 하늘 바닷가 마을 ({(from == VillageZones.Zone.Tour ? FareTour : FareCity)}G)", $"🏡 Village ({(from == VillageZones.Zone.Tour ? FareTour : FareCity)}G)"), new Color(0.45f, 0.78f, 0.55f), () => Travel(VillageZones.Zone.None, from == VillageZones.Zone.Tour ? FareTour : FareCity)));
            if (from != VillageZones.Zone.City) list.Add((Loc.T($"🏙 제주 시내 — 은행·옷가게·음식점·청음샵·박물관 ({FareCity}G)", $"🏙 Jeju City ({FareCity}G)"), new Color(0.40f, 0.62f, 0.92f), () => Travel(VillageZones.Zone.City, FareCity)));
            if (from != VillageZones.Zone.Tour) list.Add((Loc.T($"🏝 중몬관광단지 — 해변·주상절벽·폭포·식물원 ({FareTour}G)", $"🏝 Jungmon Resort ({FareTour}G)"), new Color(1f, 0.62f, 0.28f), () => Travel(VillageZones.Zone.Tour, FareTour)));
            list.Add((Loc.T("안 탈래", "Not now"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(Loc.T("🚌 201번 해안 버스", "🚌 Coastal bus No. 201"), Loc.T($"어디로 갈까? 지갑 {Save.stats.money:N0}G", $"Where to? {Save.stats.money:N0}G"), list.ToArray());
        }
        void Travel(VillageZones.Zone to, int fare)
        {
            if (Save == null || _busy) return;
            if (Save.stats.money < fare) { _hud.Bubble(Loc.T("버스 기사", "Driver"), Loc.T($"버스비 {fare}G 가 모자라요.", $"The fare is {fare}G.")); return; }
            StartCoroutine(TravelCo(to, fare));
        }
        IEnumerator TravelCo(VillageZones.Zone to, int fare)
        {
            _busy = true; CancelAuto(false);
            Save.stats.money -= fare; _gm.Persist(); RefreshStatus();
            yield return FadeScreen(true, 0.35f);
            if (_interior != null) { _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_") || s.id == "counter" || s.id.StartsWith("hosp_") || s.id.StartsWith("in_")); Destroy(_interior.gameObject); _interior = null; _interiorKind = null; }
            Vector3 dst; float yaw;
            if (to == VillageZones.Zone.City) { dst = VillageZones.CityStop + new Vector3(-2.5f, 0f, 1.5f); yaw = 270f; }
            else if (to == VillageZones.Zone.Tour) { dst = VillageZones.TourStop + new Vector3(0f, 0f, -2.5f); yaw = 180f; }
            else { dst = VillageWorld.Ground(VillageEast.StopX - 2.2f, VillageEast.StopZ); yaw = 270f; }
            Teleport(dst, yaw); _camYaw = _camYawTarget = yaw; SnapCamera();
            // 도착 연출: 버스가 서 있다가 떠난다
            var bus = VillageWorld.SpawnScaled("Obs3_Bus", transform, 0f, 0f, 0f, 3f);
            if (bus != null)
            {
                var side = to == VillageZones.Zone.None ? Vector3.right * 3.2f : Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 3.2f;
                bus.transform.position = new Vector3(dst.x + side.x, VillageWorld.Height(dst.x, dst.z) + 0.05f, dst.z + side.z);
                bus.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, yaw, 0f) * Vector3.left);
                StartCoroutine(BusLeave(bus.transform));
            }
            yield return null; yield return FadeScreen(false, 0.4f);
            CoastToast.Pop(Loc.T($"🚌 {VillageZones.Name(to)} 도착!", $"🚌 Arrived: {VillageZones.Name(to)}"));
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
            if (to == VillageZones.Zone.City) _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("와, 시내다! 빌딩이 높네. 은행·옷가게·국수집·청음샵·박물관이 다 있어.", "Downtown! Bank, boutique, noodles, records, museum…"));
            if (to == VillageZones.Zone.Tour) _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("중몬이다! 해변·주상절벽·폭포·식물원… 오늘은 푹 쉬어야지.", "Jungmon! Beach, cliffs, falls, garden… time to relax."));
            _busy = false;
        }
        IEnumerator BusLeave(Transform bus)
        {
            yield return new WaitForSeconds(2.2f);
            float t = 0f, v = 0f;
            while (t < 5f && bus != null) { t += Time.deltaTime; v = Mathf.Min(9f, v + 4f * Time.deltaTime); bus.position += bus.forward * v * Time.deltaTime; yield return null; }
            if (bus != null) Destroy(bus.gameObject);
        }

        // ── 광산 ─────────────────────────────────────────────────────────
        void EnterMine() { if (_busy) return; StartCoroutine(MineWarp(true)); }
        void ExitMine() { if (_busy) return; StartCoroutine(MineWarp(false)); }
        IEnumerator MineWarp(bool enter)
        {
            _busy = true; yield return FadeScreen(true, 0.3f);
            if (enter) { VillageZones.RefreshOre(Save); Teleport(VillageZones.MineEntry + Vector3.forward * 1.2f, 0f); }
            else Teleport(VillageWorld.Ground(VillageZones.MineMouth.x, VillageZones.MineMouth.y - 2.6f), 180f);
            _camYaw = _camYawTarget = enter ? 0f : 180f; SnapCamera();
            yield return null; yield return FadeScreen(false, 0.35f); _busy = false;
            if (enter)
            {
                int left = 14 - (Save != null ? Save.mineDone.Count : 0);
                CoastToast.Pop(Loc.T("⛏ 오름 광산", "⛏ Oreum Mine"));
                _hud.Bubble(Loc.T("광부 할아버지", "Old miner"), Loc.T($"곡괭이 들고 반짝이는 광맥 옆에서 두드려. 이번 주 남은 광맥 {left}곳. 보라빛·크림빛은 귀한 거야!", $"Pickaxe by a glowing vein. {left} veins left this week."));
                VillageDex.Talk(Save, VillageDex.NpcIndex("miner"), 1);
            }
        }
        bool TryMineOre()
        {
            if (VillageZones.At(_player.position) != VillageZones.Zone.Mine || Save == null) return false;
            float best = 2.4f; VillageZones.OreNode? hit = null;
            foreach (var n in VillageZones.OreNodes) { if (n.t == null || Save.mineDone.Contains(n.idx)) continue; float d = Vector3.Distance(new Vector3(n.t.position.x, 0f, n.t.position.z), new Vector3(_player.position.x, 0f, _player.position.z)); if (d < best) { best = d; hit = n; } }
            if (!hit.HasValue) { Swing(); CoastToast.Show(Loc.T("탕— 반짝이는 광맥 곁에서 휘둘러야 한다.", "Clang — stand by a glowing vein.")); return true; }
            StartCoroutine(MineOreCo(hit.Value)); return true;
        }
        IEnumerator MineOreCo(VillageZones.OreNode n)
        {
            _busy = true; int kind = VillageZones.NodeKind(Save, n.idx);
            for (int h = 0; h < 2; h++)
            {
                Swing(); CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit);
                VillagePang.Burst(n.t.position + Vector3.up * 1.0f, VillageZones.NodeColor(kind), Color.white, 0.7f);
                yield return new WaitForSeconds(0.35f);
            }
            string id; int cnt = 1;
            float r = UnityEngine.Random.value;
            switch (kind)
            {
                case 0: id = "ore_iron"; cnt = UnityEngine.Random.Range(1, 3) + (Tier(3) >= 2 ? 1 : 0); break;   // 198차: 곡괭이 등급
                case 1: id = "ore_silver"; break;
                case 2: id = "ore_gold"; break;
                case 3: r = Mathf.Min(0.999f, r + 0.1f * Tier(3)); id = r < 0.6f ? "gem_amethyst" : r < 0.9f ? "gem_jade" : "gem_ruby"; break;
                default: id = r < 0.3f ? "fossil_ammonite" : r < 0.55f ? "fossil_trilobite" : r < 0.85f ? "fossil_fern" : "fossil_shark"; break;
            }
            Save.mineDone.Add(n.idx); LifeItems.Add(Save, id, cnt); if (UnityEngine.Random.value < 0.5f) LifeItems.Add(Save, "mat_stone", 1);
            MissionTick(VillageMission.Kind.Mine); MissionTick(VillageMission.Kind.Ore);
            VillageZones.RefreshOre(Save); _gm.Persist(); RefreshStatus();
            var def = LifeItems.Get(id); string nm = def.HasValue ? LifeItems.Name(def.Value) : id;
            if (kind >= 2) CoastToast.Pop(Loc.T($"✨ {nm} 발견!", $"✨ Found {nm}!")); else CoastToast.Show(Loc.T($"⛏ {nm} ×{cnt} — 시내 축협·마트에서 팔 수 있다", $"⛏ {nm} ×{cnt}"));
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.6f);
            _busy = false;
        }

        // ── 관광지 활동 ────────────────────────────────────────────────────
        void TourAct(int i)
        {
            if (Save == null) return; VillageDex.Ensure(Save);
            int stamp = Save.week * 4 + Save.phaseIndex;
            if (Save.tourStamp[i] == stamp) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("여긴 방금 즐겼어. 다른 곳도 둘러보자!", "Just did this. Let's see another spot!")); return; }
            int[] st = { 15, 10, 10, 8, 6 };
            Save.tourStamp[i] = stamp; Save.stats.stress = Mathf.Max(0, Save.stats.stress - st[i]); if (i == 4) Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 20); Save.stats.Clamp();
            if (i == 1) MissionTick(VillageMission.Kind.TourPhoto); if (i == 2) MissionTick(VillageMission.Kind.TourFalls);
            _gm.Persist(); RefreshStatus();
            string[] say = { "발가락 사이로 모래가 사르르. 파도 소리만 들린다.", "찰칵! 육각 기둥이 바다까지 이어져 있다. 사진 한 장 저장.", "물보라가 무지개를 만든다. 선녀 다리 위에서 소원을 빌었다.", "유리 돔 안이 초록으로 가득. 선인장 꽃이 피었다.", "호텔 정원 벤치. 야자수 그림자 아래서 한숨 돌렸다." };
            VillagePang.Burst(_player.position + Vector3.up * 1.4f, new Color(0.55f, 0.85f, 1f), Color.white, 1.2f);
            CoastToast.Pop(Loc.T($"😌 스트레스 −{st[i]}", $"😌 Stress −{st[i]}"));
            _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T(say[i], say[i]));
        }

        // ── 가게 실내 ──────────────────────────────────────────────────────
        bool _mapHid211;
        void BuildZoneRoom(string kind)
        {
            if (_interior == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW;
            var host = RoomHost("ZoneRoom_" + kind);
            var wood = new Color(0.62f, 0.44f, 0.30f); var dark = new Color(0.30f, 0.26f, 0.24f);
            string model = "Npc_Cafe", line = "", npcKey = ""; Action menu = null; string title = "";
            switch (kind)
            {
                case "boutique":
                    TintRoom(new Color(0.95f, 0.88f, 0.86f), new Color(1f, 0.93f, 0.95f), new Color(0.90f, 0.45f, 0.62f));
                    for (int r = -1; r <= 1; r += 2) { float rx = ox + r * (rw * 0.5f - 0.8f); RB(host, "Rack", new Vector3(rx, fy + 1.5f, oz - 0.5f), new Vector3(0.06f, 0.06f, 4.2f), dark); for (int k = 0; k < 10; k++) RB(host, "Cloth", new Vector3(rx, fy + 1.0f, oz - 2.4f + k * 0.4f), new Vector3(0.5f, 0.9f, 0.3f), Color.HSVToRGB(((r + 1) * 5 + k) / 20f, 0.45f, 0.95f)); }
                    RB(host, "Mirror", new Vector3(ox + rw * 0.5f - 0.15f, fy + 1.3f, oz - 1f), new Vector3(0.06f, 2.2f, 1.2f), new Color(0.80f, 0.92f, 1f));
                    model = "Npc_Florist"; npcKey = "boutique"; line = Loc.T("어서 오세요! 제주 감성 옷 입어 보실래요?", "Welcome! Try some Jeju style?"); menu = BoutiqueMenu; title = Loc.T("👗 점원 · 옷 사기 / 갈아입기", "👗 Clerk · Buy / wear"); break;
                case "records":
                    TintRoom(new Color(0.30f, 0.26f, 0.30f), new Color(0.45f, 0.40f, 0.60f), new Color(0.95f, 0.75f, 0.30f));
                    for (int r = 0; r < 2; r++) for (int k = 0; k < 10; k++) RB(host, "Sleeve", new Vector3(ox - 4.2f + k * 0.5f, fy + 1.0f + r * 0.7f, oz + rd * 0.5f - 0.3f), new Vector3(0.42f, 0.42f, 0.04f), Color.HSVToRGB((k * 0.09f + r * 0.4f) % 1f, 0.6f, 0.9f));
                    RB(host, "Turntable", new Vector3(ox + 2.5f, fy + 0.85f, oz + 1.3f), new Vector3(0.9f, 0.12f, 0.7f), dark);
                    RB(host, "Booth", new Vector3(ox - 3f, fy + 1.1f, oz - 1.8f), new Vector3(1.4f, 2.2f, 1.4f), new Color(0.95f, 0.75f, 0.30f), true);
                    model = "Npc_Surfer"; npcKey = "dj"; line = Loc.T("섬소리에 온 걸 환영해. 이 섬의 OST, 한 곡씩 들어 봐.", "Welcome to Islesound. Hear the island OST."); menu = RecordMenu; title = Loc.T("🎧 청음 · OST 듣기", "🎧 Listen · OST"); break;
                case "noodle":
                case "pork":
                    bool noodle = kind == "noodle";
                    TintRoom(noodle ? new Color(0.80f, 0.70f, 0.55f) : new Color(0.45f, 0.38f, 0.34f), noodle ? new Color(1f, 0.95f, 0.85f) : new Color(0.95f, 0.88f, 0.80f), noodle ? new Color(0.85f, 0.45f, 0.20f) : new Color(0.30f, 0.26f, 0.26f));
                    for (int tIdx = 0; tIdx < 4; tIdx++) { float tx = ox - 3f + (tIdx % 2) * 3.2f, tz = oz - 2f + (tIdx / 2) * 2.2f; RB(host, "Table", new Vector3(tx, fy + 0.72f, tz), new Vector3(1.4f, 0.08f, 0.9f), wood, true); if (!noodle) RB(host, "Grill", new Vector3(tx, fy + 0.8f, tz), new Vector3(0.5f, 0.06f, 0.5f), dark); else RB(host, "Bowl", new Vector3(tx, fy + 0.82f, tz), new Vector3(0.3f, 0.12f, 0.3f), Color.white); }
                    RB(host, "Kitchen", new Vector3(ox + 2f, fy + 0.5f, oz + 1.6f), new Vector3(4.5f, 1f, 0.8f), new Color(0.85f, 0.85f, 0.88f), true);
                    model = noodle ? "Npc_Keeper" : "Npc_FisherBoy"; npcKey = kind; line = noodle ? Loc.T("고기국수 한 그릇 할래? 국물 끝내줘.", "Pork noodles? The broth's amazing.") : Loc.T("흑돼지는 두껍게 구워야 제맛!", "Thick-cut black pork!");
                    menu = noodle ? (Action)(() => FoodMenu(NoodleMenu, Loc.T("🍜 올레 고기국수", "🍜 Olle Noodles"))) : () => FoodMenu(PorkMenu, Loc.T("🥩 돔베 흑돼지", "🥩 Dombe Black Pork")); title = Loc.T("🍽 주문하기 (HP 회복)", "🍽 Order (restore HP)"); break;
                case "mart":
                    TintRoom(new Color(0.88f, 0.90f, 0.86f), new Color(0.95f, 0.98f, 0.95f), new Color(0.30f, 0.62f, 0.42f));
                    for (int r = 0; r < 3; r++) for (int lv = 0; lv < 3; lv++) { RB(host, "MartShelf", new Vector3(ox - 3f + r * 3f, fy + 0.4f + lv * 0.6f, oz - 0.5f), new Vector3(2.4f, 0.06f, 0.8f), new Color(0.85f, 0.85f, 0.88f)); for (int k = 0; k < 5; k++) RB(host, "Goods", new Vector3(ox - 3.9f + r * 3f + k * 0.45f, fy + 0.6f + lv * 0.6f, oz - 0.5f), new Vector3(0.35f, 0.3f, 0.4f), Color.HSVToRGB(((r * 15 + lv * 5 + k) * 0.071f) % 1f, 0.5f, 0.95f)); }
                    model = "Npc_Cafe"; npcKey = "mart"; line = Loc.T("시내 마트엔 없는 게 없어요!", "We have everything!"); menu = MartMenu; title = Loc.T("🛒 계산대 · 장보기", "🛒 Checkout · Shop"); break;
                case "museum":
                    TintRoom(new Color(0.78f, 0.72f, 0.64f), new Color(0.93f, 0.90f, 0.84f), new Color(0.45f, 0.36f, 0.28f));
                    int di = 0;
                    foreach (var e in VillageDex.Items)
                    {
                        if (!VillageDex.Donatable(e.key)) continue;
                        float px = ox - 4.2f + (di % 7) * 1.4f, pz = oz - 2.4f + (di / 7) * 1.7f; di++;
                        RB(host, "Pedestal", new Vector3(px, fy + 0.45f, pz), new Vector3(0.7f, 0.9f, 0.7f), new Color(0.95f, 0.94f, 0.90f), true);
                        if (Save.dexDonated.Contains(e.key)) RB(host, "Exhibit", new Vector3(px, fy + 1.05f, pz), new Vector3(0.35f, 0.3f, 0.35f), e.cat == 1 ? new Color(0.35f, 0.55f, 0.30f) : e.cat == 3 ? new Color(0.85f, 0.78f, 0.60f) : Color.HSVToRGB((di * 0.13f) % 1f, 0.6f, 0.95f));
                    }
                    model = "Npc_Keeper"; npcKey = "curator"; line = Loc.T("빈 진열장을 채워 줄래요? 벌레·광물·화석을 기증받아요.", "Help fill our cases — bugs, minerals, fossils."); menu = MuseumMenu; title = Loc.T("🏛 큐레이터 · 기증하기 / 도감", "🏛 Curator · Donate / Encyclopedia"); break;
                case "market":
                    TintRoom(new Color(0.70f, 0.60f, 0.45f), new Color(0.95f, 0.90f, 0.80f), new Color(0.55f, 0.42f, 0.25f));
                    for (int k = 0; k < 4; k++) RB(host, "Pen", new Vector3(ox - 3.5f + k * 2.3f, fy + 0.5f, oz - 2f), new Vector3(0.1f, 1f, 2.2f), wood, true);
                    RB(host, "HayBale", new Vector3(ox - 3f, fy + 0.4f, oz + 1.5f), new Vector3(1.2f, 0.8f, 0.8f), new Color(0.90f, 0.80f, 0.45f));
                    model = "Npc_FisherBoy"; npcKey = "market"; line = Loc.T("송아지·흑돼지 새끼 들였어. 키워서 오면 좋은 값 쳐 줄게!", "Calves and piglets in. Raise them, I'll pay well!"); menu = MarketMenu; title = Loc.T("🐄 축협 · 가축 사고팔기 / 농산물 팔기", "🐄 Market · Livestock / Produce"); break;
                case "souv":
                    TintRoom(new Color(0.92f, 0.86f, 0.74f), new Color(1f, 0.96f, 0.88f), new Color(0.98f, 0.58f, 0.18f));
                    for (int k = 0; k < 8; k++) RB(host, "SouvShelf", new Vector3(ox - 3.5f + k * 1f, fy + 1.1f, oz + rd * 0.5f - 0.35f), new Vector3(0.8f, 0.5f, 0.4f), k % 2 == 0 ? new Color(1f, 0.60f, 0.15f) : new Color(0.45f, 0.42f, 0.40f));
                    model = "Npc_Florist"; npcKey = "souv"; line = Loc.T("중몬 오신 기념! 귤 초콜릿 하나 어때요?", "A Jungmon souvenir? Tangerine chocolate!"); menu = SouvenirMenu; title = Loc.T("🎁 기념품 사기", "🎁 Buy souvenirs"); break;
                case "teddy":   // 196차: 테디베어 박물관 — 진열장 곰들
                    TintRoom(new Color(0.90f, 0.80f, 0.68f), new Color(1f, 0.95f, 0.88f), new Color(0.62f, 0.40f, 0.26f));
                    for (int k = 0; k < 7; k++)
                    {
                        float bx = ox - 3.6f + k * 1.2f, bz = oz + rd * 0.5f - 0.5f; var tc = k % 3 == 0 ? new Color(0.72f, 0.50f, 0.32f) : k % 3 == 1 ? new Color(0.95f, 0.85f, 0.70f) : new Color(0.55f, 0.35f, 0.25f);
                        RB(host, "TeddyCase", new Vector3(bx, fy + 0.45f, bz), new Vector3(1f, 0.9f, 0.7f), new Color(0.85f, 0.92f, 0.98f));
                        RB(host, "TeddyB", new Vector3(bx, fy + 1.15f, bz), new Vector3(0.45f, 0.5f, 0.4f), tc); RB(host, "TeddyH", new Vector3(bx, fy + 1.55f, bz), new Vector3(0.36f, 0.34f, 0.34f), tc);
                    }
                    model = "Npc_Cafe"; npcKey = "teddy"; line = Loc.T("테디곰 박물관에 오신 걸 환영해요. 해녀 곰도 있답니다.", "Welcome! We even have a haenyeo bear."); menu = TeddyMenu; title = Loc.T("🧸 테디곰 박물관", "🧸 Teddy Cub Museum"); break;
                case "gacha":   // 198차: 럭키 가챠샵 — 캡슐 기계 줄
                    TintRoom(new Color(0.98f, 0.86f, 0.92f), new Color(1f, 0.95f, 0.98f), new Color(0.95f, 0.45f, 0.62f));
                    for (int k = 0; k < 5; k++)
                    {
                        float gx = ox - 3.6f + k * 1.8f, gz = oz + rd * 0.5f - 0.6f; var gc = Color.HSVToRGB(k / 5f, 0.45f, 1f);
                        RB(host, "GachaBase", new Vector3(gx, fy + 0.45f, gz), new Vector3(0.9f, 0.9f, 0.7f), gc, true);
                        RB(host, "GachaDome", new Vector3(gx, fy + 1.25f, gz), new Vector3(0.8f, 0.7f, 0.6f), new Color(0.85f, 0.95f, 1f));
                        for (int q = 0; q < 4; q++) RB(host, "Capsule", new Vector3(gx - 0.2f + (q % 2) * 0.4f, fy + 1.1f + (q / 2) * 0.25f, gz - 0.05f), new Vector3(0.18f, 0.18f, 0.18f), Color.HSVToRGB((k * 4 + q) / 20f, 0.6f, 1f));
                    }
                    model = "Npc_Florist"; npcKey = "gacha"; line = Loc.T("젤리 넣고 돌려 봐! 전설 캡슐이 나올지도?", "Pop in jelly and turn the handle!"); menu = GachaMenu; title = Loc.T("럭키 가챠샵", "Lucky Gacha"); break;
                case "workshop":   // 198차: 탐라 도구 공방 — 모루·화로·도구 걸이
                    TintRoom(new Color(0.45f, 0.38f, 0.34f), new Color(0.80f, 0.72f, 0.62f), new Color(0.85f, 0.45f, 0.20f));
                    RB(host, "Anvil", new Vector3(ox - 2.5f, fy + 0.5f, oz + 0.8f), new Vector3(1.0f, 0.5f, 0.5f), new Color(0.25f, 0.25f, 0.28f), true);
                    RB(host, "AnvilBase", new Vector3(ox - 2.5f, fy + 0.2f, oz + 0.8f), new Vector3(0.5f, 0.4f, 0.4f), new Color(0.35f, 0.30f, 0.26f));
                    RB(host, "Forge", new Vector3(ox + 3.2f, fy + 0.8f, oz + rd * 0.5f - 0.7f), new Vector3(1.6f, 1.6f, 1.0f), new Color(0.40f, 0.36f, 0.34f), true);
                    RB(host, "ForgeFire", new Vector3(ox + 3.2f, fy + 0.8f, oz + rd * 0.5f - 1.22f), new Vector3(0.8f, 0.5f, 0.05f), new Color(1f, 0.55f, 0.15f));
                    for (int k = 0; k < 5; k++) RB(host, "ToolRack", new Vector3(ox - 3.5f + k * 1.1f, fy + 1.6f, oz + rd * 0.5f - 0.12f), new Vector3(0.12f, 1.0f, 0.05f), k % 2 == 0 ? new Color(0.62f, 0.44f, 0.30f) : new Color(0.75f, 0.76f, 0.80f));
                    model = "Npc_Keeper"; npcKey = "workshop"; line = Loc.T("재료만 있으면 뭐든 벼려 주지. 튼튼한 → 은빛 → 황금!", "Bring materials — Sturdy, Silver, Golden!"); menu = WorkshopMenu; title = Loc.T("탐라 도구 공방", "Tamra Tool Workshop"); break;
                default: return;
            }
            RB(host, "ShopCounter", new Vector3(ox, fy + 0.4f, oz + 1.5f), new Vector3(3.2f, 0.8f, 0.8f), wood, true);
            DressRoom(host, fy);   // 211차: 상자 소품 → 블렌더 소품
            RoomNpc(host, model, new Vector3(ox, fy, oz + 2.4f), 180f, line);
            int ni = VillageDex.NpcIndex(npcKey); var m = menu;
            _spots.Add(new Spot { id = "in_" + kind, title = title, pos = new Vector3(ox, fy, oz + 0.5f), radius = 2.0f, on = () => { if (ni >= 0) VillageDex.Talk(Save, ni, 1); m(); } });
            BuildingOutline.Attach(host, 0.018f);
        }

        static readonly BrunchItem[] NoodleMenu = {
            new BrunchItem { ko = "🍜 고기국수", en = "🍜 Pork noodles", price = 160, hp = 60, stress = 4, col = new Color(0.95f, 0.75f, 0.45f) },
            new BrunchItem { ko = "🥣 몸국", en = "🥣 Momguk", price = 130, hp = 45, stress = 3, col = new Color(0.55f, 0.72f, 0.50f) },
            new BrunchItem { ko = "🌯 빙떡", en = "🌯 Bingtteok", price = 60, hp = 20, stress = 2, col = new Color(0.90f, 0.85f, 0.70f) },
        };
        static readonly BrunchItem[] PorkMenu = {
            new BrunchItem { ko = "🥩 흑돼지 근고기", en = "🥩 Thick black pork", price = 480, hp = 130, stress = 8, col = new Color(0.70f, 0.35f, 0.30f) },
            new BrunchItem { ko = "🍲 흑돼지 김치찌개", en = "🍲 Pork kimchi stew", price = 260, hp = 70, stress = 5, col = new Color(0.90f, 0.40f, 0.30f) },
            new BrunchItem { ko = "🍖 돔베고기", en = "🍖 Dombe pork", price = 360, hp = 95, stress = 6, col = new Color(0.85f, 0.60f, 0.45f) },
        };
        void FoodMenu(BrunchItem[] items, string title)
        {
            var list = new List<(string, Color, Action)>();
            foreach (var it in items) { var item = it; list.Add((Loc.T($"{item.ko}  ·  HP +{item.hp}  ·  {item.price}G", $"{item.en}  ·  HP +{item.hp}  ·  {item.price}G"), item.col, () => EatBrunch(item))); }
            list.Add((Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(title, Loc.T($"HP {Save.stats.stamina}/{PlayerStats.StatMax}  ·  지갑 {Save.stats.money:N0}G", $"HP {Save.stats.stamina} · {Save.stats.money:N0}G"), list.ToArray());
        }

        // ── 옷 ───────────────────────────────────────────────────────────
        struct Outfit { public string id, ko; public int price; public Color col; public int hat; }   // hat: 0 없음(윗옷 색) 1 캡 2 밀짚 3 털모자 4 귤 모자 5 머리핀 6 목도리
        static readonly Outfit[] Outfits = {
            new Outfit { id = "top_tangerine", ko = "귤빛 후드", price = 450, col = new Color(1f, 0.58f, 0.18f) },
            new Outfit { id = "top_sea", ko = "바다 파랑 셔츠", price = 450, col = new Color(0.30f, 0.62f, 0.92f) },
            new Outfit { id = "top_canola", ko = "유채 노랑 원피스", price = 600, col = new Color(1f, 0.86f, 0.25f) },
            new Outfit { id = "top_basalt", ko = "현무암 그레이 재킷", price = 800, col = new Color(0.40f, 0.42f, 0.46f) },
            new Outfit { id = "hat_cap", ko = "제주 캡모자", price = 300, col = new Color(0.25f, 0.55f, 0.45f), hat = 1 },
            new Outfit { id = "hat_tangerine", ko = "감귤 모자 (중몬 한정)", price = 500, col = new Color(1f, 0.55f, 0.12f), hat = 4 },
            new Outfit { id = "pin_canola", ko = "유채꽃 머리핀 (봄 축제 한정)", price = 400, col = new Color(1f, 0.88f, 0.2f), hat = 5 },
            new Outfit { id = "hat_straw", ko = "밀짚모자 (여름 축제 한정)", price = 400, col = new Color(0.95f, 0.85f, 0.55f), hat = 2 },
            new Outfit { id = "scarf_orange", ko = "귤빛 목도리 (가을 축제 한정)", price = 400, col = new Color(1f, 0.55f, 0.15f), hat = 6 },
            new Outfit { id = "hat_knit", ko = "털모자 (겨울 축제 한정)", price = 400, col = new Color(0.85f, 0.25f, 0.30f), hat = 3 },
        };
        static int OutfitIndex(string id) { for (int i = 0; i < Outfits.Length; i++) if (Outfits[i].id == id) return i; return -1; }
        void BoutiqueMenu()
        {
            var list = new List<(string, Color, Action)>();
            for (int i = 0; i < 5; i++) list.Add(OutfitRow(i));
            foreach (var id in Save.outfits) { int k = OutfitIndex(id); if (k >= 5) list.Add(OutfitRow(k)); }
            list.Add((Loc.T("원래 옷으로", "Default outfit"), new Color(0.7f, 0.7f, 0.75f), () => { Save.outfit = ""; _gm.Persist(); ApplyOutfit(); }));
            list.Add((Loc.T("🧥 새 옷 세트(생활용 12주)", "🧥 Clothes set (12 weeks)"), new Color(0.55f, 0.70f, 0.90f), OpenShop));
            _hud.Choice(Loc.T("👗 탐라 부티크", "👗 Tamra Boutique"), Loc.T($"한 번 사면 언제든 갈아입을 수 있어요 · 지갑 {Save.stats.money:N0}G", $"Buy once, wear anytime · {Save.stats.money:N0}G"), list.ToArray());
        }
        (string, Color, Action) OutfitRow(int i)
        {
            var o = Outfits[i]; bool own = Save.outfits.Contains(o.id); bool on = Save.outfit == o.id;
            string label = on ? Loc.T($"✔ {o.ko} (입는 중)", $"✔ {o.ko} (wearing)") : own ? Loc.T($"{o.ko} · 갈아입기", $"{o.ko} · wear") : Loc.T($"{o.ko} · {o.price}G", $"{o.ko} · {o.price}G");
            return (label, o.col, () => BuyWear(i));
        }
        void BuyWear(int i)
        {
            var o = Outfits[i];
            if (!Save.outfits.Contains(o.id))
            {
                if (Save.stats.money < o.price) { _hud.Bubble(Loc.T("점원", "Clerk"), Loc.T($"{o.price}G 가 필요해요.", $"That's {o.price}G.")); return; }
                Save.stats.money -= o.price; Save.outfits.Add(o.id); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            }
            Save.outfit = o.id; _gm.Persist(); RefreshStatus(); ApplyOutfit();
            VillagePang.Burst(_player.position + Vector3.up * 1.2f, o.col, Color.white, 1.3f);
            CoastToast.Pop(Loc.T($"👗 {o.ko}!", $"👗 {o.ko}!"));
        }
        Transform _outfitVis; readonly Dictionary<Material, Color> _origCol = new Dictionary<Material, Color>();
        void ApplyOutfit()
        {
            if (_rigT == null || Save == null) return;
            if (_outfitVis != null) Destroy(_outfitVis.gameObject);
            int k = OutfitIndex(Save.outfit); var o = k >= 0 ? Outfits[k] : default;
            foreach (var smr in _rigT.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (var m in smr.materials)
                {
                    string n = m.name;
                    if (!(n.Contains("Jacket") || n.Contains("Shirt") || n.Contains("Top") || n.Contains("Hood") || n.Contains("Cloth"))) continue;
                    string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : m.HasProperty("_Color") ? "_Color" : null; if (prop == null) continue;
                    if (!_origCol.ContainsKey(m)) _origCol[m] = m.GetColor(prop);
                    m.SetColor(prop, k >= 0 && o.hat == 0 ? o.col : _origCol[m]);
                }
            if (k < 0 || o.hat == 0) return;
            Transform head = _anim != null && _anim.avatar != null && _anim.avatar.isHuman ? _anim.GetBoneTransform(HumanBodyBones.Head) : null;
            var root = new GameObject("OutfitVis").transform; _outfitVis = root; root.SetParent(head != null ? head : _rigT, false); root.localPosition = head != null ? new Vector3(0f, 0.22f, 0f) : new Vector3(0f, 1.3f, 0f);
            var mat = CoastMaterials.CreateLit(o.col, 0.1f);
            GameObject P(PrimitiveType t, Vector3 lp, Vector3 s) { var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(root, false); g.transform.localPosition = lp; g.transform.localScale = s; g.GetComponent<MeshRenderer>().sharedMaterial = mat; return g; }
            switch (o.hat)
            {
                case 1: P(PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.2f, 0.36f)); P(PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.2f), new Vector3(0.26f, 0.03f, 0.18f)); break;
                case 2: P(PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0f), new Vector3(0.6f, 0.01f, 0.6f)); P(PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.3f, 0.08f, 0.3f)); break;
                case 3: P(PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), new Vector3(0.38f, 0.26f, 0.38f)); P(PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.12f); break;
                case 4: P(PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.4f, 0.3f, 0.4f)); var leaf = P(PrimitiveType.Sphere, new Vector3(0.05f, 0.2f, 0f), new Vector3(0.14f, 0.05f, 0.08f)); leaf.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.3f, 0.6f, 0.25f)); break;
                case 5: P(PrimitiveType.Sphere, new Vector3(0.14f, 0.0f, 0.06f), Vector3.one * 0.09f); P(PrimitiveType.Sphere, new Vector3(0.18f, 0.02f, 0.02f), Vector3.one * 0.07f); break;
                case 6: root.localPosition = head != null ? new Vector3(0f, -0.2f, 0f) : new Vector3(0f, 1.0f, 0f); P(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.34f, 0.04f, 0.34f)); P(PrimitiveType.Cube, new Vector3(0.08f, -0.12f, 0.14f), new Vector3(0.08f, 0.2f, 0.03f)); break;
            }
        }

        // ── 청음샵(OST) ──────────────────────────────────────────────────
        void RecordMenu()
        {
            var list = new List<(string, Color, Action)>();
            foreach (var tr in RecordTable.All)
            {
                var t = tr; bool on = _recSrc != null && _recSrc.isPlaying && _recPlaying == t.num;
                list.Add(((on ? "⏸ " : "▶ ") + Loc.T($"{t.ko} — {t.noteKo}", $"{t.en} — {t.noteEn}"), on ? new Color(0.95f, 0.55f, 0.35f) : new Color(0.55f, 0.50f, 0.85f), () => { if (on) StopRecord(); else PlayRecord(t.num, t.Clip); }));
            }
            list.Add((Loc.T("⏹ 끄기", "⏹ Stop"), new Color(0.6f, 0.6f, 0.66f), StopRecord));
            _hud.Choice(Loc.T("🎧 섬소리 청음샵 · OST", "🎧 Islesound · OST"), Loc.T("헤드폰을 쓰고 한 곡씩. 가게를 나가면 음악이 멈춰요.", "Put on the headphones. Music stops when you leave."), list.ToArray());
        }
        void PlayRecord(int num, string clipKey)
        {
            var clip = CoastBgmLibrary.Load(clipKey);
            if (clip == null) { _hud.Bubble(Loc.T("DJ", "DJ"), Loc.T("앗, 그 판은 지금 대여 중이야.", "Oops, that record's out.")); return; }
            if (_recSrc == null) { _recSrc = gameObject.AddComponent<AudioSource>(); _recSrc.playOnAwake = false; _recSrc.loop = true; _recSrc.spatialBlend = 0f; }
            TitleAudio.SetBedVolume(0f); _recSrc.clip = clip; _recSrc.volume = 0.9f; _recSrc.Play(); _recPlaying = num;
            CoastToast.Show(Loc.T($"🎧 ♪ {RecordTable.TitleOf(num)}", $"🎧 ♪ {RecordTable.TitleOf(num)}"));
        }
        void StopRecord() { if (_recSrc != null) _recSrc.Stop(); _recPlaying = 0; TitleAudio.SetBedVolume(0.85f); }

        // ── 마트 · 박물관 · 축협 · 기념품 ─────────────────────────────────
        void MartMenu()
        {
            _hud.Choice(Loc.T("🛒 시내 마트", "🛒 City Mart"), Loc.T($"마을상점보다 물건이 많아요 · 지갑 {Save.stats.money:N0}G", $"More than the village shop · {Save.stats.money:N0}G"), new (string, Color, Action)[] {
                (Loc.T("🥬 식료품·약·생활용품 (전 품목)", "🥬 Groceries · medicine · care"), new Color(0.45f, 0.78f, 0.55f), OpenShop),
                (Loc.T($"🎁 선물 과자 세트 · 120G (누구나 조금 좋아함)", "🎁 Gift snack box · 120G"), new Color(0.98f, 0.62f, 0.72f), () => BuyItem("gift_snack", 120)),
                (Loc.T($"🐝 벌통 세트 · 800G (유채꽃밭에 설치, 최대 3)", "🐝 Beehive kit · 800G"), new Color(1f, 0.80f, 0.30f), BuyHive),
                (Loc.T("💰 물건 팔기", "💰 Sell"), new Color(0.98f, 0.80f, 0.30f), SellMenu),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        void BuyItem(string id, int price)
        {
            if (Save.stats.money < price) { CoastToast.Show(Loc.T($"돈이 모자라요 ({price}G)", $"Need {price}G")); return; }
            Save.stats.money -= price; LifeItems.Add(Save, id, 1); _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            var d = LifeItems.Get(id); CoastToast.Show(Loc.T($"🛍 {(d.HasValue ? LifeItems.Name(d.Value) : id)} 샀다 (−{price}G)", $"🛍 Bought (−{price}G)"));
        }
        void MuseumMenu()
        {
            _hud.Choice(Loc.T("🏛 민속자연사박물관", "🏛 Museum"), Loc.T($"기증 {Save.dexDonated.Count}점 · 한 점마다 40G + 별조각 1, 5점마다 보너스", $"{Save.dexDonated.Count} donated"), new (string, Color, Action)[] {
                (Loc.T("🎁 가방의 새 전시품 모두 기증하기", "🎁 Donate everything new"), new Color(0.85f, 0.65f, 0.40f), Donate),
                (Loc.T("📖 도감 보기", "📖 Encyclopedia"), new Color(0.55f, 0.62f, 0.92f), () => OpenDex(1)),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        void Donate()
        {
            int n = 0;
            foreach (var e in VillageDex.Items)
            {
                if (!VillageDex.Donatable(e.key) || Save.dexDonated.Contains(e.key) || LifeItems.Count(Save, e.key) <= 0) continue;
                LifeItems.Take(Save, e.key, 1); Save.dexDonated.Add(e.key); n++;
            }
            if (n == 0) { _hud.Bubble(Loc.T("큐레이터", "Curator"), Loc.T("새로 기증할 게 없네요. 광산·언덕·바닷가에서 찾아 와요!", "Nothing new. Try the mine, hills, beach!")); return; }
            int money = n * 40 + (Save.dexDonated.Count / 5) * 0; Save.stats.money += money; Save.starShards += n; Save.starShardsTotal += n;
            if (Save.dexDonated.Count % 5 < n) { Save.stats.money += 200; money += 200; }
            _gm.Persist(); RefreshStatus(); VillagePang.Burst(_player.position + Vector3.up * 1.4f, new Color(1f, 0.85f, 0.4f), Color.white, 1.5f);
            CoastToast.Pop(Loc.T($"🏛 {n}점 기증! +{money}G · 별조각 +{n} (전시 {Save.dexDonated.Count}점)", $"🏛 Donated {n}! +{money}G"));
            VillageDex.AddFriend(Save, VillageDex.NpcIndex("curator"), 4 * n);
        }
        void MarketMenu()
        {
            UpdateAnimals();
            int grownCow = 0, grownPig = 0; foreach (var a in Save.calfAge) if (a >= 4) grownCow++; foreach (var a in Save.pigAge) if (a >= 3) grownPig++;
            _hud.Choice(Loc.T("🐄 축협 가축시장", "🐄 Livestock Market"), Loc.T($"축사: 소 {Save.calfAge.Length}/3 (다 큼 {grownCow}) · 흑돼지 {Save.pigAge.Length}/4 (다 큼 {grownPig}) · 소 4주·돼지 3주면 다 큼", $"Cows {Save.calfAge.Length}/3 · Pigs {Save.pigAge.Length}/4"), new (string, Color, Action)[] {
                (Loc.T("🐮 송아지 사기 · 700G (축사로 배달, 다 크면 1,500G · 우유)", "🐮 Calf · 700G"), new Color(0.90f, 0.85f, 0.75f), () => BuyAnimal(true)),
                (Loc.T("🐷 흑돼지 새끼 사기 · 350G (다 크면 750G)", "🐷 Piglet · 350G"), new Color(0.45f, 0.40f, 0.42f), () => BuyAnimal(false)),
                (Loc.T($"💰 다 큰 소 팔기 ({grownCow}마리 × 1,500G)", $"💰 Sell cows ({grownCow})"), new Color(0.98f, 0.75f, 0.30f), () => SellAnimals(true)),
                (Loc.T($"💰 다 큰 흑돼지 팔기 ({grownPig}마리 × 750G)", $"💰 Sell pigs ({grownPig})"), new Color(0.98f, 0.75f, 0.30f), () => SellAnimals(false)),
                (Loc.T("🍊 농산물 팔기 (감귤·꿀·귤청·우유·달걀 +20 %)", "🍊 Sell produce (+20%)"), new Color(1f, 0.62f, 0.28f), SellProduce),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        void BuyAnimal(bool cow)
        {
            int price = cow ? 700 : 350; var arr = cow ? Save.calfAge : Save.pigAge; int cap = cow ? 3 : 4;
            if (arr.Length >= cap) { _hud.Bubble(Loc.T("축협 아저씨", "Market man"), Loc.T("축사가 꽉 찼어. 다 큰 녀석을 먼저 팔아.", "The barn is full.")); return; }
            if (Save.stats.money < price) { _hud.Bubble(Loc.T("축협 아저씨", "Market man"), Loc.T($"{price}G 가 필요해.", $"Need {price}G.")); return; }
            Save.stats.money -= price; var list = new List<int>(arr) { 0 }; if (cow) Save.calfAge = list.ToArray(); else Save.pigAge = list.ToArray();
            if (Save.animalWeek < 0) Save.animalWeek = Save.week;
            _gm.Persist(); RefreshStatus(); RefreshBarn(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            CoastToast.Pop(cow ? Loc.T("🐮 송아지가 동쪽 축사로 갔다!", "🐮 A calf went to the barn!") : Loc.T("🐷 흑돼지 새끼가 축사로 갔다!", "🐷 A piglet went to the barn!"));
        }
        void SellAnimals(bool cow)
        {
            UpdateAnimals(); int grow = cow ? 4 : 3, price = cow ? 1500 : 750; /* 195차 밸런스 */ var keep = new List<int>(); int sold = 0;
            foreach (var a in cow ? Save.calfAge : Save.pigAge) { if (a >= grow) sold++; else keep.Add(a); }
            if (sold == 0) { _hud.Bubble(Loc.T("축협 아저씨", "Market man"), Loc.T("아직 다 큰 녀석이 없네. 조금 더 키워 와.", "None are grown yet.")); return; }
            if (cow) Save.calfAge = keep.ToArray(); else Save.pigAge = keep.ToArray();
            Save.stats.money += sold * price; _gm.Persist(); RefreshStatus(); RefreshBarn(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.8f);
            CoastToast.Pop(Loc.T($"💰 {(cow ? "소" : "흑돼지")} {sold}마리 팔았다! +{sold * price:N0}G", $"💰 Sold {sold}! +{sold * price:N0}G"));
        }
        void SellProduce()
        {
            string[] ids = { "fruit_tangerine", "fruit_hallabong", "honey_jar", "jam_tangerine", "ing_milk", "ing_egg" };
            int got = 0;
            foreach (var id in ids) { int n = LifeItems.Count(Save, id); if (n <= 0) continue; int p = 0; foreach (var (pid, pr) in VillageSell.Prices) if (pid == id) p = pr; p = Mathf.RoundToInt(p * 1.2f); got += VillageSell.Sell(Save, id, n, p); }
            if (got <= 0) { _hud.Bubble(Loc.T("축협 아저씨", "Market man"), Loc.T("감귤·꿀·귤청·우유·달걀을 가져오면 마을보다 20% 더 쳐 줄게.", "Bring produce — I pay 20% more.")); return; }
            SoldToast(got);
        }
        void SouvenirMenu()
        {
            _hud.Choice(Loc.T("🎁 중몬 기념품 가게", "🎁 Souvenir shop"), Loc.T($"기념품을 사면 마을 부탁도 해결! · 지갑 {Save.stats.money:N0}G", $"{Save.stats.money:N0}G"), new (string, Color, Action)[] {
                (Loc.T("🍫 귤 초콜릿 · 120G", "🍫 Tangerine chocolate · 120G"), new Color(1f, 0.62f, 0.25f), () => { BuyItem("souv_choco", 120); MissionTick(VillageMission.Kind.TourSouvenir); }),
                (Loc.T("🗿 하르방 인형 · 300G", "🗿 Hareubang doll · 300G"), new Color(0.55f, 0.52f, 0.50f), () => { BuyItem("souv_doll", 300); MissionTick(VillageMission.Kind.TourSouvenir); }),
                (Save.outfits.Contains("hat_tangerine") ? Loc.T("🍊 감귤 모자 쓰기", "🍊 Wear tangerine hat") : Loc.T("🍊 감귤 모자 · 500G (중몬 한정 옷)", "🍊 Tangerine hat · 500G"), new Color(1f, 0.55f, 0.12f), () => { BuyWear(OutfitIndex("hat_tangerine")); MissionTick(VillageMission.Kind.TourSouvenir); }),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }

        // ── 과수원 · 벌통 · 축사(동쪽) ──────────────────────────────────────
        const int OrchardPrice = 1500;
        void OrchardMenu()
        {
            VillageDex.Talk(Save, VillageDex.NpcIndex("brunch"), 0);
            if (!Save.orchardOwned)
            {
                _hud.Choice(Loc.T("🍊 돌담 귤 과수원", "🍊 Orchard"), Loc.T($"귤나무 9그루. 사면 한 주마다 감귤을 수확(가을엔 두 배)하고 귤청을 담글 수 있다. {OrchardPrice:N0}G · 지갑 {Save.stats.money:N0}G", $"9 trees. Weekly harvest. {OrchardPrice:N0}G"), new (string, Color, Action)[] {
                    (Save.stats.money >= OrchardPrice ? Loc.T($"✅ 과수원 사기 (−{OrchardPrice:N0}G)", "✅ Buy") : Loc.T($"💸 {OrchardPrice - Save.stats.money:N0}G 모자람", "💸 Not enough"), Save.stats.money >= OrchardPrice ? new Color(0.45f, 0.78f, 0.50f) : new Color(0.6f, 0.6f, 0.66f), () => { if (Save.stats.money < OrchardPrice) return; Save.stats.money -= OrchardPrice; Save.orchardOwned = true; Save.orchardWeek = -1; _gm.Persist(); RefreshStatus(); CoastToast.Pop(Loc.T("🍊 내 과수원이 생겼다!", "🍊 The orchard is yours!")); VillagePang.Burst(_player.position + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.2f), Color.white, 2f); }),
                    (Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), () => { }),
                });
                return;
            }
            bool ready = Save.orchardWeek != Save.week;
            _hud.Choice(Loc.T("🍊 내 과수원", "🍊 My orchard"), Loc.T($"감귤 {LifeItems.Count(Save, "fruit_tangerine")} · 한라봉 {LifeItems.Count(Save, "fruit_hallabong")} · 꿀 {LifeItems.Count(Save, "honey_jar")} · 귤청 {LifeItems.Count(Save, "jam_tangerine")}", ""), new (string, Color, Action)[] {
                (ready ? Loc.T("🧺 이번 주 수확하기", "🧺 Harvest this week") : Loc.T("🧺 이번 주는 이미 땄다 (다음 주에)", "🧺 Harvested (next week)"), ready ? new Color(1f, 0.62f, 0.25f) : new Color(0.6f, 0.6f, 0.66f), HarvestOrchard),
                (Loc.T("🫙 귤청 담그기 (감귤 3 + 꿀 1 → 귤청, 180G에 팔림)", "🫙 Make syrup (3 tangerines + 1 honey)"), new Color(0.95f, 0.75f, 0.35f), MakeJam),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        void HarvestOrchard()
        {
            if (Save.orchardWeek == Save.week) return;
            bool autumn = Timeline.SeasonOf(Save.week) == SeasonKind.Autumn; var ev = VillageSeason.Now(Save);
            int n = UnityEngine.Random.Range(10, 17) * (autumn ? 2 : 1); if (ev != null && ev.id == 2) n += 6;
            int hb = UnityEngine.Random.value < 0.4f ? UnityEngine.Random.Range(1, 4) : 0;
            Save.orchardWeek = Save.week; LifeItems.Add(Save, "fruit_tangerine", n); if (hb > 0) LifeItems.Add(Save, "fruit_hallabong", hb);
            _gm.Persist(); RefreshStatus();
            for (int i = 0; i < 8; i++) StartCoroutine(DropOrange(_player.position + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 2.2f, UnityEngine.Random.Range(0.5f, 2.5f)), new Color(1f, 0.55f, 0.12f), 0.18f));
            CoastToast.Pop(Loc.T($"🍊 감귤 {n}개{(hb > 0 ? $" · 한라봉 {hb}개" : "")} 수확!{(autumn ? " (가을 두 배)" : "")}", $"🍊 {n} tangerines!"));
        }
        void MakeJam()
        {
            int t = LifeItems.Count(Save, "fruit_tangerine"), h = LifeItems.Count(Save, "honey_jar"); int n = Mathf.Min(t / 3, h);
            if (n <= 0) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("감귤 3개와 꿀 1병이 있어야 귤청을 담글 수 있어. 꿀은 벌통에서!", "Need 3 tangerines + 1 honey.")); return; }
            LifeItems.Take(Save, "fruit_tangerine", n * 3); LifeItems.Take(Save, "honey_jar", n); LifeItems.Add(Save, "jam_tangerine", n); _gm.Persist(); RefreshStatus();
            CoastToast.Pop(Loc.T($"🫙 귤청 {n}병 완성!", $"🫙 {n} jars of syrup!"));
        }
        void BuyHive()
        {
            if (Save.hives >= 3) { CoastToast.Show(Loc.T("벌통은 3개까지야.", "Max 3 hives.")); return; }
            if (Save.stats.money < 800) { CoastToast.Show(Loc.T("벌통 세트는 800G.", "A hive kit is 800G.")); return; }
            Save.stats.money -= 800; Save.hives++; if (Save.hiveWeek < 0) Save.hiveWeek = Save.week; _gm.Persist(); RefreshStatus(); RefreshHives();
            CoastToast.Pop(Loc.T($"🐝 벌통 설치! (유채꽃밭 옆 {Save.hives}/3)", $"🐝 Hive set up! ({Save.hives}/3)"));
        }
        void HiveMenu()
        {
            if (Save.hives <= 0) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("유채꽃밭 옆 빈 받침대… 시내 마트에서 벌통 세트(800G)를 사 오면 여기서 꿀을 뜰 수 있어.", "Buy a hive kit at the City Mart (800G).")); return; }
            bool ready = Save.hiveWeek != Save.week;
            if (!ready) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T($"벌들이 열심히 일하는 중. 다음 주에 꿀을 뜨자. (벌통 {Save.hives})", "The bees are busy. Next week!")); return; }
            bool spring = Timeline.SeasonOf(Save.week) == SeasonKind.Spring; int n = Save.hives * (spring ? 4 : 2);
            Save.hiveWeek = Save.week; LifeItems.Add(Save, "honey_jar", n); _gm.Persist(); RefreshStatus();
            VillagePang.Burst(_player.position + Vector3.up * 1.2f, new Color(1f, 0.82f, 0.25f), Color.white, 1.2f);
            CoastToast.Pop(Loc.T($"🍯 꿀 {n}병!{(spring ? " (유채꽃 철 두 배)" : "")}", $"🍯 {n} jars of honey!"));
        }
        void UpdateAnimals()
        {
            if (Save == null) return; VillageDex.Ensure(Save);
            if (Save.animalWeek < 0) { Save.animalWeek = Save.week; return; }
            int w = Save.week - Save.animalWeek; if (w < 0) w += Timeline.Weeks; if (w <= 0) return;
            for (int i = 0; i < Save.calfAge.Length; i++) Save.calfAge[i] += w; for (int i = 0; i < Save.pigAge.Length; i++) Save.pigAge[i] += w;
            Save.animalWeek = Save.week;
        }
        void BarnMenu()
        {
            UpdateAnimals();
            int grownCow = 0; foreach (var a in Save.calfAge) if (a >= 4) grownCow++;
            bool milk = grownCow > 0 && Save.milkWeek != Save.week;
            _hud.Choice(Loc.T("🐄 축사", "🐄 Barn"), Loc.T($"소 {Save.calfAge.Length}/3 · 흑돼지 {Save.pigAge.Length}/4 — 새끼는 시내 축협에서 사고, 다 크면 거기서 판다.", $"Cows {Save.calfAge.Length} · Pigs {Save.pigAge.Length}"), new (string, Color, Action)[] {
                (milk ? Loc.T($"🥛 우유 짜기 (다 큰 소 {grownCow}마리 × 2)", "🥛 Milk the cows") : Loc.T("🥛 우유는 다 큰 소가 한 주에 한 번", "🥛 Milk: grown cows, weekly"), milk ? new Color(0.95f, 0.95f, 0.90f) : new Color(0.6f, 0.6f, 0.66f), () => { if (!milk) return; Save.milkWeek = Save.week; LifeItems.Add(Save, "ing_milk", grownCow * 2); _gm.Persist(); RefreshStatus(); CoastToast.Pop(Loc.T($"🥛 우유 {grownCow * 2}병!", $"🥛 {grownCow * 2} milk!")); }),
                (Loc.T("🫳 쓰다듬기 (스트레스 −3)", "🫳 Pet them (stress −3)"), new Color(0.98f, 0.70f, 0.75f), () => { Save.stats.stress = Mathf.Max(0, Save.stats.stress - 3); _gm.Persist(); RefreshStatus(); VillagePang.Burst(_player.position + Vector3.up, new Color(1f, 0.7f, 0.8f), Color.white, 0.9f); }),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        Transform _barnAnimals, _hiveVis;
        void RefreshBarn()
        {
            if (_world == null || Save == null) return;
            if (_barnAnimals != null) Destroy(_barnAnimals.gameObject);
            _barnAnimals = new GameObject("BarnAnimals").transform; _barnAnimals.SetParent(_world, false);
            var pen = VillageEast.PenCenter; var white = CoastMaterials.CreateLit(new Color(0.96f, 0.95f, 0.92f)); var black = CoastMaterials.CreateLit(new Color(0.16f, 0.15f, 0.16f)); var pink = CoastMaterials.CreateLit(new Color(0.95f, 0.70f, 0.72f));
            for (int i = 0; i < Save.calfAge.Length + Save.pigAge.Length; i++)
            {
                bool cow = i < Save.calfAge.Length; int age = cow ? Save.calfAge[i] : Save.pigAge[i - Save.calfAge.Length];
                float s = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(age / (cow ? 4f : 3f)));
                var a = new GameObject(cow ? "Cow" : "Pig").transform; a.SetParent(_barnAnimals, false);
                a.position = VillageWorld.Ground(pen.x - 2.5f + (i % 4) * 1.7f, pen.y - 1.2f + (i / 4) * 2f); a.rotation = Quaternion.Euler(0f, i * 70f, 0f); a.localScale = Vector3.one * s;
                GameObject P(PrimitiveType t, Vector3 lp, Vector3 sc, Material m) { var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(a, false); g.transform.localPosition = lp; g.transform.localScale = sc; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g; }
                if (cow)
                {
                    P(PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.6f, 0.8f), white).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    P(PrimitiveType.Sphere, new Vector3(0.15f, 0.9f, 0.1f), new Vector3(0.4f, 0.3f, 0.5f), black); P(PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0.75f), new Vector3(0.45f, 0.42f, 0.5f), white);
                    P(PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0.98f), new Vector3(0.3f, 0.2f, 0.12f), pink);
                    for (int l = 0; l < 4; l++) P(PrimitiveType.Cylinder, new Vector3(l % 2 == 0 ? -0.22f : 0.22f, 0.25f, l < 2 ? -0.4f : 0.4f), new Vector3(0.14f, 0.25f, 0.14f), white);
                }
                else
                {
                    P(PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.55f, 0.95f), black); P(PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0.5f), new Vector3(0.22f, 0.06f, 0.22f), black).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    for (int l = 0; l < 4; l++) P(PrimitiveType.Cylinder, new Vector3(l % 2 == 0 ? -0.18f : 0.18f, 0.12f, l < 2 ? -0.28f : 0.28f), new Vector3(0.1f, 0.12f, 0.1f), black);
                }
                a.gameObject.AddComponent<IdleWander>();
            }
            BuildingOutline.Attach(_barnAnimals, 0.02f);
        }
        void RefreshHives()
        {
            if (_world == null || Save == null) return;
            if (_hiveVis != null) Destroy(_hiveVis.gameObject);
            _hiveVis = new GameObject("Hives").transform; _hiveVis.SetParent(_world, false);
            var wood = CoastMaterials.CreateLit(new Color(0.95f, 0.85f, 0.55f)); var roof = CoastMaterials.CreateLit(new Color(0.55f, 0.40f, 0.25f));
            for (int i = 0; i < 3; i++)
            {
                var g = VillageWorld.Ground(VillageEast.HiveX, 30f + i * 2.4f);
                var stand = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(stand.GetComponent<Collider>()); stand.transform.SetParent(_hiveVis, false); stand.transform.position = g + Vector3.up * 0.2f; stand.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f); stand.GetComponent<MeshRenderer>().sharedMaterial = roof;
                if (i >= Save.hives) continue;
                for (int k = 0; k < 3; k++) { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); if (k > 0) Destroy(b.GetComponent<Collider>()); b.transform.SetParent(_hiveVis, false); b.transform.position = g + Vector3.up * (0.6f + k * 0.34f); b.transform.localScale = new Vector3(0.8f, 0.32f, 0.8f); b.GetComponent<MeshRenderer>().sharedMaterial = wood; }
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(r.GetComponent<Collider>()); r.transform.SetParent(_hiveVis, false); r.transform.position = g + Vector3.up * 1.62f; r.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f); r.GetComponent<MeshRenderer>().sharedMaterial = roof;
            }
        }

        // ── 계절 이벤트 부스 ────────────────────────────────────────────────
        void EventBoothMenu()
        {
            var ev = VillageSeason.Now(Save); if (ev == null) return;
            int oi = OutfitIndex(ev.outfit); var o = Outfits[oi]; bool own = Save.outfits.Contains(o.id);
            int stamp = Save.week * 4 + Save.phaseIndex; bool played = Save.tourStamp.Length > 4 && false;
            _hud.Choice(Loc.T($"🎪 {ev.ko}", $"🎪 {ev.en}"), Loc.T($"{ev.from}~{ev.to}주차 한정! · 지갑 {Save.stats.money:N0}G", $"Weeks {ev.from}-{ev.to} only!"), new (string, Color, Action)[] {
                (Loc.T($"😋 {ev.foodKo} · HP +{ev.foodHp} · {ev.foodPrice}G", $"😋 {ev.foodKo} · {ev.foodPrice}G"), ev.col, () => EatBrunch(new BrunchItem { ko = ev.foodKo, en = ev.foodKo, price = ev.foodPrice, hp = ev.foodHp, stress = ev.foodStress, col = ev.col })),
                (own ? Loc.T($"👗 {o.ko} 입기", "Wear") : Loc.T($"👗 {o.ko} · {o.price}G (이번 축제에서만)", $"👗 {o.ko} · {o.price}G"), o.col, () => BuyWear(oi)),
                (Loc.T("🎆 축제 즐기기 (스트레스 −12, 페이즈마다)", "🎆 Enjoy the festival (stress −12)"), new Color(0.98f, 0.55f, 0.70f), () => FestivalFun(ev)),
                (Loc.T("나가기", "Leave"), new Color(0.6f, 0.6f, 0.66f), () => { }),
            });
        }
        int _festStamp = -1;
        void FestivalFun(VillageSeason.Ev ev)
        {
            int stamp = Save.week * 4 + Save.phaseIndex; if (_festStamp == stamp) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("방금 실컷 놀았어! 다음에 또 오자.", "Just had fun! Later.")); return; }
            _festStamp = stamp; Save.stats.stress = Mathf.Max(0, Save.stats.stress - 12); _gm.Persist(); RefreshStatus();
            StartCoroutine(Fireworks(ev.col));
            string[] say = { "유채꽃 사진 액자 앞에서 찰칵! 노란 바다 같다.", "밤바다 위로 불꽃이 퐁퐁. 모래가 아직 따뜻하다.", "귤을 톡 따서 바로 까먹었다. 새콤달콤!", "붕어빵 꼬리부터 먹는 파. 입김이 하얗다." };
            _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T(say[ev.id], say[ev.id]));
        }
        IEnumerator Fireworks(Color c)
        {
            for (int i = 0; i < 8; i++)
            {
                var p = _player.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), 6f + UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(3f, 8f));
                VillagePang.Burst(p, i % 2 == 0 ? c : new Color(1f, 0.55f, 0.75f), Color.white, 2.5f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.3f);
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── 도감 · 선물 ────────────────────────────────────────────────────
        void OpenDex(int tab = 0)
        {
            if (Save == null) return; _busy = true;
            VillageDex.Show(Save, GiftTo, () => { _busy = false; RefreshStatus(); }, tab);
        }
        void GiftTo(int npc)
        {
            var n = VillageDex.Npcs[npc];
            var owned = LifeItems.ListOwned(Save);
            var list = new List<(string, Color, Action)>();
            owned.Sort((a, b) => (b.def.id == n.likes ? 1 : 0).CompareTo(a.def.id == n.likes ? 1 : 0));
            int shown = 0;
            foreach (var (def, cnt) in owned)
            {
                if (shown++ >= 7) break; var d = def;
                bool like = d.id == n.likes || d.id == "gift_snack";
                list.Add((Loc.T($"{(d.id == n.likes ? "💗 " : "")}{LifeItems.Name(d)} ×{cnt}", LifeItems.Name(d)), d.id == n.likes ? new Color(0.98f, 0.55f, 0.70f) : new Color(0.90f, 0.85f, 0.75f), () =>
                {
                    if (!LifeItems.Take(Save, d.id, 1)) return;
                    int amt = d.id == n.likes ? 10 : d.id == "gift_snack" ? 5 : 2;
                    bool bday = BirthdayWeek(npc) == Save.week; if (bday) amt *= 3;   // 205차: 생일이면 세 배
                    VillageDex.AddFriend(Save, npc, amt); _gm.Persist(); RefreshStatus();
                    CoastToast.Show(Loc.T($"🎁 {n.ko}에게 {LifeItems.Name(d)} — {(bday ? $"생일 선물! 🎂 +{amt}" : d.id == n.likes ? "정말 좋아한다! 💗 +10" : $"고마워한다 +{amt}")} (우편으로 보냄)", $"🎁 Sent to {n.en} (+{amt})"));
                    VillageDex.Show(Save, GiftTo, () => { _busy = false; RefreshStatus(); }, 4);
                }));
            }
            if (list.Count == 0) { CoastToast.Show(Loc.T("가방에 선물할 게 없다.", "Nothing to give.")); return; }
            VillageDex.Close(); _busy = false;
            list.Add((Loc.T("취소", "Cancel"), new Color(0.6f, 0.6f, 0.66f), () => OpenDex(4)));
            _hud.Choice(Loc.T($"🎁 {n.ko}에게 선물 (우편)", $"🎁 Gift to {n.en}"), Loc.T($"좋아하는 것: {n.likesKo} (+10) · 선물 과자 +5 · 그 외 +2 · 지금 {VillageDex.Hearts(VillageDex.Friend(Save, npc))}", ""), list.ToArray());
        }

        /// 마을 사람과 대화할 때(OnTalk) — 호감도 +2(페이즈마다), 호감이 오르면 말이 달라진다
        void FriendTalkVillager(int ni)
        {
            if (ni < 0 || ni > 5 || Save == null) return;
            VillageDex.Talk(Save, ni, 2);
            if (VillageDex.Friend(Save, ni) >= 20) NpcSay(ni, VillageDex.Line(Save, ni));
            _gm.Persist();
        }

        /// 구역 안 자동 이동: 도로망 대신 곧장 걷기
        bool ZoneAutoMove()
        {
            var z = VillageZones.At(_player.position); if (z == VillageZones.Zone.None) return false;
            var list = new List<(string, Color, Action)>();
            void Go(string ko, Vector3 p, Color c) => list.Add((ko, c, () => { _walkTo = p; CoastToast.Show(Loc.T("🧭 걸어가는 중… 조이스틱을 밀면 멈춘다", "🧭 Walking…")); }));
            Vector3 Door(Transform h) { foreach (var hs in VillageWorld.Houses) if (hs.house == h) return hs.door + h.forward * 0.4f; return h != null ? h.position : _player.position; }
            if (z == VillageZones.Zone.City)
            {
                Go(Loc.T("🚌 시내 정류장", "🚌 Stop"), VillageZones.CityStop, new Color(0.40f, 0.62f, 0.92f));
                Go(Loc.T("🏦 은행", "🏦 Bank"), Door(VillageZones.CityBank), new Color(0.35f, 0.50f, 0.80f));
                Go(Loc.T("👗 탐라 부티크", "👗 Boutique"), Door(VillageZones.Boutique), new Color(0.90f, 0.45f, 0.62f));
                Go(Loc.T("🎧 섬소리 청음샵", "🎧 Records"), Door(VillageZones.Records), new Color(0.55f, 0.50f, 0.85f));
                Go(Loc.T("🍜 올레 고기국수", "🍜 Noodles"), Door(VillageZones.Noodle), new Color(0.95f, 0.65f, 0.35f));
                Go(Loc.T("🥩 돔베 흑돼지", "🥩 Pork"), Door(VillageZones.Pork), new Color(0.70f, 0.40f, 0.35f));
                Go(Loc.T("🛒 시내 마트", "🛒 Mart"), Door(VillageZones.Mart), new Color(0.45f, 0.78f, 0.55f));
                Go(Loc.T("🏛 민속자연사박물관", "🏛 Museum"), Door(VillageZones.Museum), new Color(0.75f, 0.60f, 0.40f));
                Go(Loc.T("🐄 축협 가축시장", "🐄 Market"), Door(VillageZones.Market), new Color(0.60f, 0.48f, 0.30f));
            }
            else if (z == VillageZones.Zone.Tour)
            {
                Go(Loc.T("🚌 중몬 정류장", "🚌 Stop"), VillageZones.TourStop, new Color(0.40f, 0.62f, 0.92f));
                string[] nm = { "🏖 색동해변", "📸 주상절벽대", "💧 천재연 폭포", "🌺 여미니 식물원", "🪑 호텔 정원" };
                for (int i = 0; i < 5; i++) Go(nm[i], VillageZones.TourSpots[i], new Color(1f, 0.62f, 0.28f));
                Go(Loc.T("🎁 기념품 가게", "🎁 Souvenirs"), Door(VillageZones.Souvenir), new Color(0.98f, 0.62f, 0.72f));
            }
            else Go(Loc.T("🪜 광산 출구", "🪜 Exit"), VillageZones.MineEntry, new Color(0.55f, 0.45f, 0.35f));
            _hud.Choice(Loc.T($"🧭 {VillageZones.Name(z)} 안에서 이동", "🧭 Move"), null, list.ToArray());
            return true;
        }

        // ── 개발 ────────────────────────────────────────────────────────
        public void DevTravel(int z) { DevLeaveInterior(); Save.stats.money += z == 2 ? FareTour : FareCity; Travel(z == 1 ? VillageZones.Zone.City : z == 2 ? VillageZones.Zone.Tour : VillageZones.Zone.None, z == 2 ? FareTour : FareCity); }
        int _devSpot;
        /// 개발: 관광지 5곳 → 시내 3곳 순서로 순간이동(부를 때마다 다음)
        public void DevNextSpot()
        {
            DevLeaveInterior(); var C = VillageZones.CityC;
            Vector3[] pts = { VillageZones.TourSpots[0] + new Vector3(0f, 0f, 6f), VillageZones.TourSpots[1] + new Vector3(-6f, 0f, 2f), VillageZones.TourSpots[2] + new Vector3(8f, 0f, 0f), VillageZones.TourSpots[3] + new Vector3(0f, 0f, -6f), VillageZones.TourSpots[4], C + new Vector3(-30f, 0f, 4f), C + new Vector3(0f, 0f, 18f), C + new Vector3(20f, 0f, -4f) };
            float[] yaws = { 180f, 100f, 270f, 0f, 180f, 60f, 0f, 300f };
            int i = _devSpot++ % pts.Length; Teleport(pts[i], yaws[i]); _camYaw = _camYawTarget = yaws[i]; SnapCamera();
            Debug.LogWarning("[195] spot " + i + " " + pts[i]);
        }
        public void DevMine() { DevLeaveInterior(); EnterMine(); }
        public void DevDex() { DevLeaveInterior(); OpenDex(0); }
        public void DevEnterZone(string which)
        {
            DevLeaveInterior();
            Transform h = which == "records" ? VillageZones.Records : which == "boutique" ? VillageZones.Boutique : which == "museum" ? VillageZones.Museum : which == "noodle" ? VillageZones.Noodle : which == "market" ? VillageZones.Market : which == "mart" ? VillageZones.Mart : which == "bank" ? VillageZones.CityBank : which == "teddy" ? VillageZones.Teddy : which == "gacha" ? VillageZones.Gacha : which == "workshop" ? VillageZones.Workshop : VillageZones.Souvenir;
            if (h == null) return; foreach (var hs in VillageWorld.Houses) if (hs.house == h) { EnterHouse(hs.house, hs.name, hs.door); return; }
        }
        public void DevMineOre() { _pick = true; ApplyTool(); if (VillageZones.OreNodes.Count > 0) { var n = VillageZones.OreNodes[0]; Teleport(n.t.position + new Vector3(0f, 0f, -1.6f), 0f); TryMineOre(); } }
        public void DevGiveAnimals() { Save.calfAge = new[] { 5, 1 }; Save.pigAge = new[] { 3 }; Save.animalWeek = Save.week; Save.hives = 2; Save.orchardOwned = true; _gm.Persist(); RefreshBarn(); RefreshHives(); }
    }

    /// 축사 동물 — 제자리에서 천천히 돌며 고개 끄덕
    public class IdleWander : MonoBehaviour
    {
        Vector3 _home; float _ph;
        void Start() { _home = transform.position; _ph = UnityEngine.Random.value * 10f; }
        void Update()
        {
            float t = Time.time * 0.25f + _ph;
            var p = _home + new Vector3(Mathf.Sin(t) * 0.6f, 0f, Mathf.Cos(t * 0.7f) * 0.5f); p.y = VillageWorld.Height(p.x, p.z);
            var d = p - transform.position; transform.position = p; d.y = 0f; if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 2f);
        }
    }
}
