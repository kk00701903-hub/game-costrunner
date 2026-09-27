using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 205차(사용자: 「재미 요소 추가해줘」 — 목장이야기 비교 제안 P1·P2 중 고른 네 가지):
    ///   ① 동물 돌보기(닭·토끼·말 쓰다듬기/먹이 → 애정 ♥, 황금 달걀·말 타기) ② 농사 확장(계절 씨앗·밭 넓히기·비료 ★·비 오면 저절로 물)
    ///   ③ 하루 정산(잠들기 전 오늘 한 일 한 장) ④ 주민 하트 이벤트·생일 · 축제 대회
    public partial class VillageHub
    {
        // ── 공통 ─────────────────────────────────────────────────────────
        void InitFun205()
        {
            if (Save == null) return;
            VillageFarm.Ensure(Save);
            if (Save.npcHeartSeen == null || Save.npcHeartSeen.Length < 32) { var a = new int[32]; if (Save.npcHeartSeen != null) Array.Copy(Save.npcHeartSeen, a, Save.npcHeartSeen.Length); Save.npcHeartSeen = a; }
            if (Save.daySnapWeek != Save.week) TakeDaySnap();
            // 생일 알림(그 주 처음 들어왔을 때)
            if (Save.birthdaySeenWeek != Save.week)
            {
                int b = BirthdayNpc(Save.week);
                if (b >= 0) { Save.birthdaySeenWeek = Save.week; StartCoroutine(LaterToast(3.2f, Loc.T($"♥ 이번 주는 {VillageDex.Npcs[b].ko} 생일! 선물하면 호감이 세 배로 오른다.", $"♥ {VillageDex.Npcs[b].en}'s birthday this week! Gifts count triple."))); }
            }
        }

        int _rainStamp = -1;
        void TickFun205()
        {
            if (Save == null || _hud == null) return;
            bool locked = _busy || _hud.Locked || CinematicPlayer.IsPlaying;
            // ② 비 오는 날 텃밭 저절로 물
            if (_interior == null && _weather == WeatherKind.Rain && VillageZones.At(_player.position) == VillageZones.Zone.None)
            {
                int stamp = VillageFarm.Stamp(Save);
                if (_rainStamp != stamp)
                {
                    _rainStamp = stamp; int n = VillageFarm.RainWater(Save);
                    if (n > 0) { VillageWorld.BuildCrops(_world, Save); _gm.Persist(); CoastToast.Show(Loc.T($"● 비가 텃밭을 적셨다 — {n}칸 물 주기 끝", $"● The rain watered {n} plot(s)")); }
                }
            }
            // ① 말 타기
            TickRide();
            // ④ 하트 이벤트(호감 단계를 넘은 뒤, 팝업이 닫히면)
            if (!locked && VillageDex.PendingHeartNpc >= 0) ShowHeartEvent();
        }

        void AddFunSpots()
        {
            // ② 넓힌 밭(서쪽 칸) — 넓히기 전엔 멀리 치워 둔다
            _spots.Add(new Spot { id = "garden2", title = Loc.T("텃밭(서쪽) · 밭 칸으로 들어가자", "Garden (west) · Step onto a plot"), pos = GardenWestPos(), radius = 5.2f, on = FarmAct });
            // ④ 축제 대회 접수 — 이벤트 주간이면 부스 옆
            var ev = VillageSeason.Now(Save);
            if (ev != null && VillageSeason.Booth != null)
                _spots.Add(new Spot { id = "contest", title = Loc.T($"★ {ContestName(ev.id)} 접수", $"★ {ContestNameEn(ev.id)} entry"), pos = VillageSeason.Booth.position + new Vector3(3.6f, 0f, -1.8f), radius = 2.2f, on = () => ContestMenu(ev) });
        }

        // ── ① 동물 돌보기 ────────────────────────────────────────────────
        const int FeedPrice = 30, CarrotPrice = 50;
        bool CarePetDone(int bit) => Save.carePetWeek == Save.week && (Save.carePetMask & bit) != 0;
        bool CareFeedDone(int bit) => Save.careFeedWeek == Save.week && (Save.careFeedMask & bit) != 0;

        /// 농장 메뉴 맨 위에 붙는 돌보기 항목(닭·토끼)
        (string, Color, Action)[] CareRows()
        {
            var list = new List<(string, Color, Action)>();
            if (Save.farmChickens > 0) list.Add((Loc.T($"♥ 닭 돌보기 {VillageLivestock.HeartText(Save.loveHen)}", $"♥ Care for hens {VillageLivestock.HeartText(Save.loveHen)}"), new Color(0.98f, 0.60f, 0.72f), () => CareMenu(0)));
            if (VillageLivestock.Rabbits(Save) > 0) list.Add((Loc.T($"♥ 토끼 돌보기 {VillageLivestock.HeartText(Save.loveRabbit)}", $"♥ Care for rabbits {VillageLivestock.HeartText(Save.loveRabbit)}"), new Color(0.95f, 0.66f, 0.80f), () => CareMenu(1)));
            if (Save.farmGoldEggs > 0) list.Add((Loc.T($"★ 황금 달걀 {Save.farmGoldEggs}개 줍기", $"★ Collect {Save.farmGoldEggs} golden egg(s)"), new Color(0.98f, 0.80f, 0.30f), () => { int g = VillageLivestock.CollectGoldEggs(Save); _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin); CoastToast.Show(Loc.T($"★ 황금 달걀 {g}개! (상점에서 150G)", $"★ {g} golden egg(s)!")); }));
            return list.ToArray();
        }

        static (string, Color, Action)[] Join205((string, Color, Action)[] a, (string, Color, Action)[] b) { var r = new (string, Color, Action)[a.Length + b.Length]; a.CopyTo(r, 0); b.CopyTo(r, a.Length); return r; }
        static Texture2D[] Pad205(int n, Texture2D[] icons) { var r = new Texture2D[n + icons.Length]; icons.CopyTo(r, n); return r; }

        /// kind 0 닭 · 1 토끼
        void CareMenu(int kind)
        {
            if (Save == null) return;
            int bit = kind == 0 ? 1 : 2; int love = kind == 0 ? Save.loveHen : Save.loveRabbit;
            string nm = kind == 0 ? Loc.T("닭", "Hens") : Loc.T("토끼", "Rabbits");
            bool pet = CarePetDone(bit), fed = CareFeedDone(bit);
            bool hasFood = kind == 1 && (LifeItems.Count(Save, "crop_carrot") > 0 || LifeItems.Count(Save, "ing_veg") > 0);
            string perk = kind == 0 ? Loc.T("♥3 황금 달걀이 가끔 · ♥5 매일 달걀 +1", "♥3 golden eggs sometimes · ♥5 +1 egg daily")
                                    : Loc.T("♥5 이면 아침마다 네잎클로버(별조각 +1)", "♥5: a clover (★ shard) every morning");
            _hud.Choice(Loc.T($"♥ {nm} 돌보기  {VillageLivestock.HeartText(love)}", $"♥ {nm}  {VillageLivestock.HeartText(love)}"),
                Loc.T($"하루(잠 한 번)에 쓰다듬기·먹이 한 번씩. 안 쓰다듬고 자면 애정이 조금 준다. {perk}", perk),
                new (string, Color, Action)[] {
                    (pet ? Loc.T("● 쓰다듬기 — 오늘은 했다", "● Pet — done today") : Loc.T("● 쓰다듬기 (애정 +6)", "● Pet (+6)"), pet ? new Color(0.62f, 0.62f, 0.68f) : new Color(0.98f, 0.62f, 0.75f), pet ? (Action)null : () => DoCare(kind, false)),
                    (fed ? Loc.T("● 먹이 — 오늘은 줬다", "● Feed — done today") : kind == 0 ? Loc.T($"● 모이 주기 {FeedPrice}G (애정 +8)", $"● Feed {FeedPrice}G (+8)") : hasFood ? Loc.T("● 당근·채소 한 개 주기 (애정 +10)", "● Give a carrot / veg (+10)") : Loc.T($"● 토끼 사료 {FeedPrice}G (애정 +6)", $"● Feed {FeedPrice}G (+6)"),
                        fed ? new Color(0.62f, 0.62f, 0.68f) : new Color(0.60f, 0.80f, 0.45f), fed ? (Action)null : () => DoCare(kind, true)),
                    (Loc.T("◀ 농장으로", "◀ Back"), new Color(0.70f, 0.72f, 0.78f), LivestockAct),
                });
        }

        void DoCare(int kind, bool feed)
        {
            int bit = kind == 0 ? 1 : 2; int amt = 6;
            if (feed)
            {
                if (kind == 1 && LifeItems.Take(Save, "crop_carrot", 1)) amt = 10;
                else if (kind == 1 && LifeItems.Take(Save, "ing_veg", 1)) amt = 10;
                else { if (Save.stats.money < FeedPrice) { CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); return; } Save.stats.money -= FeedPrice; amt = kind == 0 ? 8 : 6; }
                if (Save.careFeedWeek != Save.week) { Save.careFeedWeek = Save.week; Save.careFeedMask = 0; }
                Save.careFeedMask |= bit;
            }
            else
            {
                if (Save.carePetWeek != Save.week) { Save.carePetWeek = Save.week; Save.carePetMask = 0; }
                Save.carePetMask |= bit;
            }
            int before = kind == 0 ? Save.loveHen : Save.loveRabbit; int after = Mathf.Min(100, before + amt);
            if (kind == 0) Save.loveHen = after; else Save.loveRabbit = after;
            var p = VillageWorld.Ground(VillageLivestock.CX, VillageLivestock.CZ) + Vector3.up * 1.0f;
            VillagePang.Burst(p, new Color(1f, 0.55f, 0.75f), Color.white, 1.4f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            string nm = kind == 0 ? Loc.T("닭들이", "The hens") : Loc.T("토끼들이", "The rabbits");
            string react = feed ? (kind == 0 ? Loc.T("모이를 쪼며 발등까지 따라온다", "peck and follow your feet") : Loc.T("당근 끝부터 오물오물 갉아 먹는다", "nibble from the tip"))
                                : (kind == 0 ? Loc.T("눈을 가늘게 뜨고 가만히 있는다", "close their eyes and stay still") : Loc.T("귀를 등에 딱 붙이고 손바닥에 머리를 민다", "flatten their ears and push into your palm"));
            CoastToast.Show(Loc.T($"{nm} {react}. {VillageLivestock.HeartText(after)}", $"{nm} {react}. {VillageLivestock.HeartText(after)}"));
            if (VillageLivestock.Hearts(after) > VillageLivestock.Hearts(before)) CoastToast.Pop(Loc.T($"♥ 애정 ♥{VillageLivestock.Hearts(after)}!", $"♥ Love ♥{VillageLivestock.Hearts(after)}!"));
            _gm.Persist(); RefreshStatus();
            CareMenu(kind);
        }

        // 말
        void RanchMenu()
        {
            if (Save == null) return;
            bool pet = Save.horsePetWeek == Save.week, fed = Save.horseFeedWeek == Save.week; int love = Save.loveHorse, h = VillageLivestock.Hearts(love);
            bool carrot = LifeItems.Count(Save, "crop_carrot") > 0;
            _hud.Choice(Loc.T($" 방목장  {VillageLivestock.HeartText(love)}", $" Ranch  {VillageLivestock.HeartText(love)}"),
                Loc.T("목장 주인이 말을 돌봐도 된다고 했다. ♥3 이 되면 한 마리 타고 마을을 달릴 수 있다.", "The rancher lets you care for the horses. At ♥3 you can ride one."),
                new (string, Color, Action)[] {
                    (pet ? Loc.T("● 갈기 빗어 주기 — 오늘은 했다", "● Brush — done today") : Loc.T("● 갈기 빗어 주기 (애정 +6)", "● Brush the mane (+6)"), pet ? new Color(0.62f, 0.62f, 0.68f) : new Color(0.85f, 0.62f, 0.45f), pet ? (Action)null : () => HorseCare(false)),
                    (fed ? Loc.T("● 당근 — 오늘은 줬다", "● Carrot — done today") : carrot ? Loc.T("● 가방의 당근 주기 (애정 +12)", "● Give a carrot (+12)") : Loc.T($"● 당근 사서 주기 {CarrotPrice}G (애정 +10)", $"● Buy & give a carrot {CarrotPrice}G (+10)"),
                        fed ? new Color(0.62f, 0.62f, 0.68f) : new Color(1f, 0.62f, 0.25f), fed ? (Action)null : () => HorseCare(true)),
                    (h >= 3 ? Loc.T("▶ 말 타고 달리기 (90초, 빨리 달린다)", "▶ Ride (90 s, faster)") : Loc.T($"▶ 말 타기 — ♥3 부터 (지금 ♥{h})", $"▶ Ride — needs ♥3 (now ♥{h})"), h >= 3 ? new Color(0.55f, 0.75f, 0.95f) : new Color(0.62f, 0.62f, 0.68f), h >= 3 ? (Action)StartRide : null),
                    (Loc.T("● 목장 주인과 이야기", "● Talk to the rancher"), new Color(0.72f, 0.72f, 0.78f), () => _hud.Bubble(Loc.T("목장 주인", "Rancher"), h >= 3 ? Loc.T("저 갈색 녀석이 너만 보면 울타리로 와. 나한텐 안 그래.", "That brown one comes to the fence when it sees you. Never for me.") : Loc.T("우리 말들 예쁘지? …설마 방망이로 치려는 건 아니지? 그랬다간 경찰 부른다!", "Pretty horses, right? …You're not going to hit them, are you? I'll call the police!"))),
                });
        }
        void HorseCare(bool feed)
        {
            int amt = 6;
            if (feed)
            {
                if (LifeItems.Take(Save, "crop_carrot", 1)) amt = 12;
                else { if (Save.stats.money < CarrotPrice) { CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); return; } Save.stats.money -= CarrotPrice; amt = 10; }
                Save.horseFeedWeek = Save.week;
            }
            else Save.horsePetWeek = Save.week;
            int before = Save.loveHorse; Save.loveHorse = Mathf.Min(100, before + amt);
            VillagePang.Burst(VillageRanch.Gate + Vector3.up * 1.2f, new Color(1f, 0.55f, 0.75f), Color.white, 1.4f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            CoastToast.Show(feed ? Loc.T($"말이 당근을 한입에 가져가고 코로 손바닥을 민다. {VillageLivestock.HeartText(Save.loveHorse)}", $"The horse takes the carrot and nudges your palm. {VillageLivestock.HeartText(Save.loveHorse)}")
                                 : Loc.T($"빗질할 때마다 말이 한쪽 발을 들었다 내린다. {VillageLivestock.HeartText(Save.loveHorse)}", $"The horse lifts a hoof with every stroke. {VillageLivestock.HeartText(Save.loveHorse)}"));
            if (VillageLivestock.Hearts(Save.loveHorse) > VillageLivestock.Hearts(before)) CoastToast.Pop(Loc.T($"♥ 말 애정 ♥{VillageLivestock.Hearts(Save.loveHorse)}!", $"♥ Horse ♥{VillageLivestock.Hearts(Save.loveHorse)}!"));
            _gm.Persist(); RefreshStatus(); RanchMenu();
        }

        // 말 타기: 발밑에 말 + 몸을 올리고 90초 동안 1.7배 속도
        float _rideT; Transform _rideHorse; Vector3 _rideRigPos;
        float RideMul => _rideT > 0f ? 1.7f : 1f;
        void StartRide()
        {
            if (_rideHorse != null || _player == null) return;
            _rideHorse = VillageRanch.Horse(_player, _player.position, _player.eulerAngles.y, new Color(0.55f, 0.36f, 0.22f), new Color(0.22f, 0.15f, 0.12f));
            _rideHorse.localPosition = new Vector3(0f, 0f, 0.25f); _rideHorse.localRotation = Quaternion.identity; _rideHorse.localScale = Vector3.one * 0.85f;   // 205-2: 말을 조금 작게·앞으로 — 탄 사람이 목에 가리지 않고 등 위에 보이게
            foreach (var c in _rideHorse.GetComponentsInChildren<Collider>()) Destroy(c);
            if (_rigT != null) { _rideRigPos = _rigT.localPosition; _rigT.localPosition = _rideRigPos + new Vector3(0f, 1.05f, -0.05f); }
            _rideT = 90f;
            CoastToast.Show(Loc.T("▶ 말에 올랐다! 조이스틱을 끝까지 밀면 달린다 (90초)", "▶ Riding! Push the stick all the way to gallop (90 s)"));
        }
        void TickRide()
        {
            if (_rideT <= 0f) return;
            _rideT -= Time.deltaTime;
            if (_interior != null || VillageZones.At(_player.position) != VillageZones.Zone.None) _rideT = 0f;
            if (_rideT <= 0f) EndRide();
        }
        void EndRide()
        {
            _rideT = 0f;
            if (_rideHorse != null) { Destroy(_rideHorse.gameObject); _rideHorse = null; if (_rigT != null) _rigT.localPosition = _rideRigPos; CoastToast.Show(Loc.T("말에서 내렸다. 말은 알아서 방목장으로 돌아간다.", "You hop off. The horse trots back to the ranch.")); }
        }

        // ── ② 농사 확장 ──────────────────────────────────────────────────
        Vector3 GardenWestPos() { if (Save == null) return new Vector3(9999f, 0f, 9999f); VillageFarm.Ensure(Save); if (VillageFarm.Rows <= 3) return new Vector3(9999f, 0f, 9999f); var c = VillageFarm.WestCenter; return VillageWorld.Ground(c.x, c.z); }
        /// 밭 칸 밖(텃밭 안내 자리)에서 누르면 — 밭 넓히기·제철 씨앗 안내
        void GardenMenu()
        {
            var season = Timeline.SeasonOf(Save.week);
            var rows = new List<(string, Color, Action)>();
            var sb = new System.Text.StringBuilder();
            foreach (var sd in HomeData.SeasonSeeds) if (sd.season == (int)season) sb.Append(sd.ko).Append(' ');
            rows.Add((Loc.T($"지금 제철: {sb}", $"In season: {sb}"), new Color(0.55f, 0.78f, 0.50f), (Action)null));
            if (VillageFarm.Rows < VillageFarm.MaxRows)
            {
                int next = VillageFarm.Rows + 1, price = VillageFarm.ExpandPrice[next - 1];
                bool can = Save.stats.money >= price;
                rows.Add((Loc.T($"■ 밭 넓히기 — 한 줄(3칸) 더 {price:N0}G", $"■ Expand — one more row (3) {price:N0}G"), can ? new Color(0.85f, 0.62f, 0.40f) : new Color(0.62f, 0.62f, 0.68f), () => ExpandFarm(price)));
            }
            else rows.Add((Loc.T("■ 밭이 가장 넓다 (15칸)", "■ Max size (15 plots)"), new Color(0.62f, 0.62f, 0.68f), (Action)null));
            rows.Add((Loc.T("품질 ★ = 1 + 비료 + 물을 한 번도 안 빼먹음 · ★ 만큼 더 거둔다", "★ = 1 + fertilizer + never missed water"), new Color(0.95f, 0.80f, 0.35f), (Action)null));
            _hud.Choice(Loc.T($"● 텃밭 ({VillageFarm.Tiles}칸)", $"● Garden ({VillageFarm.Tiles})"), Loc.T("밭 칸 위에 서서 누르면 씨 뿌리기·물·수확. 물을 두 주 넘게 못 받으면 시든다. 비 오는 날은 저절로 물.", "Stand on a plot to plant / water / harvest. Two weeks without water = withered. Rain waters for you."), rows.ToArray());
        }
        void ExpandFarm(int price)
        {
            if (Save.stats.money < price) { CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); return; }
            Save.stats.money -= price; Save.farmRows = Mathf.Min(VillageFarm.MaxRows, Save.farmRows + 1); VillageFarm.Ensure(Save);
            VillageWorld.BuildCrops(_world, Save); _gm.Persist(); RefreshStatus();
            var sp = _spots.Find(x => x.id == "garden2"); if (sp != null) sp.pos = GardenWestPos();
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin); VillagePang.Burst(VillageFarm.TileCenter(VillageFarm.Tiles - 2) + Vector3.up, new Color(0.75f, 0.55f, 0.35f), Color.white, 1.6f);
            CoastToast.Show(Loc.T($"■ 밭이 넓어졌다! 이제 {VillageFarm.Tiles}칸 — 울타리 서쪽 새 칸에 심자", $"■ Garden expanded to {VillageFarm.Tiles} plots (west)"));
        }
        /// 이미 물 준 칸: 비료를 줄지 묻는다(한 칸에 한 번)
        void FertilizeAsk(int tile)
        {
            if (!VillageFarm.CanFertilize(Save, tile)) { CoastToast.Show(Loc.T($"이번 페이즈엔 이미 물을 줬어. 지금 품질 {new string('★', VillageFarm.Star(Save, tile))}", "Already watered this phase.")); return; }
            _hud.Choice(Loc.T("● 비료 주기", "● Fertilize"), Loc.T($"이미 물은 줬다. 비료를 주면 수확 품질 ★+1 (지금 {new string('★', VillageFarm.Star(Save, tile))})", "Already watered. Fertilizer gives ★+1."),
                new (string, Color, Action)[] {
                    (Loc.T($"비료 뿌리기 {VillageFarm.FertPrice}G", $"Fertilize {VillageFarm.FertPrice}G"), new Color(0.60f, 0.45f, 0.30f), () => { if (VillageFarm.Fertilize(Save, tile)) { CoastToast.Show(Loc.T($"● 비료를 뿌렸다 — 품질 {new string('★', VillageFarm.Star(Save, tile))}", "● Fertilized")); AfterFarm(); } else CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); }),
                    (Loc.T("그냥 두기", "Leave it"), new Color(0.62f, 0.62f, 0.68f), null),
                });
        }

        // ── ③ 하루 정산 ─────────────────────────────────────────────────
        int FriendSum() { VillageDex.Ensure(Save); int n = 0; foreach (var f in Save.npcFriend) n += f; return n; }
        void TakeDaySnap()
        {
            Save.daySnapWeek = Save.week; Save.daySnapMoney = Save.stats.money; Save.daySnapItems = LifeItems.TotalOwned(Save);
            VillageDex.Ensure(Save); Save.daySnapDex = Save.dexSeen.Count; Save.daySnapFriend = FriendSum(); Save.daySnapShards = Save.starShards;
        }
        /// 잠들기 직전 — 오늘 한 일 한 장. 닫으면 잠든다.
        IEnumerator DaySummaryCo()
        {
            if (Save == null || _hud == null) yield break;
            if (Save.daySnapWeek != Save.week) TakeDaySnap();
            VillageFarm.Ensure(Save);
            int money = Save.stats.money - Save.daySnapMoney, items = LifeItems.TotalOwned(Save) - Save.daySnapItems;
            int dex = Save.dexSeen.Count - Save.daySnapDex, friend = FriendSum() - Save.daySnapFriend, shards = Save.starShards - Save.daySnapShards;
            int ripe = 0, thirsty = 0, planted = 0;
            for (int i = 0; i < VillageFarm.Tiles; i++) { if (VillageFarm.SeedOf(Save, i) == null) continue; planted++; if (VillageFarm.Bloomed(Save, i)) ripe++; else if (VillageFarm.CanWater(Save, i)) thirsty++; }
            var rows = new List<(string, Color, Action)>();
            Color info = new Color(0.78f, 0.70f, 0.60f);
            rows.Add((Loc.T($"● 돈 {(money >= 0 ? "+" : "")}{money:N0}G   ·   ● 가방 {(items >= 0 ? "+" : "")}{items}개", $"● {(money >= 0 ? "+" : "")}{money:N0}G · ● {(items >= 0 ? "+" : "")}{items}"), money >= 0 ? new Color(0.95f, 0.72f, 0.30f) : info, (Action)null));
            if (dex > 0 || shards > 0) rows.Add((Loc.T($"● 도감 새 발견 {dex}   ·   ★ 별조각 +{Mathf.Max(0, shards)}", $"● New {dex} · ★ +{Mathf.Max(0, shards)}"), new Color(0.55f, 0.70f, 0.95f), (Action)null));
            { var cg = CgSummary(); if (cg != null) rows.Add(("♥ " + cg, new Color(0.98f, 0.62f, 0.72f), (Action)null)); }   // 214차
            if (friend > 0) rows.Add((Loc.T($"♥ 마을 사람 호감 +{friend}", $"♥ Friendship +{friend}"), new Color(0.98f, 0.60f, 0.72f), (Action)null));
            if (planted > 0) rows.Add((Loc.T($"● 텃밭 {planted}칸 — 다 자람 {ripe} · 물 필요 {thirsty}", $"● {planted} planted — ripe {ripe} · thirsty {thirsty}"), thirsty > 0 ? new Color(0.55f, 0.75f, 0.95f) : new Color(0.55f, 0.78f, 0.50f), (Action)null));
            if (Save.farmChickens > 0 || VillageLivestock.Rabbits(Save) > 0)
                rows.Add((Loc.T($"● 닭 {VillageLivestock.HeartText(Save.loveHen)} · 토끼 {VillageLivestock.HeartText(Save.loveRabbit)}{(CarePetDone(1) || CarePetDone(2) ? "" : " — 오늘 못 쓰다듬음")}", $"● Hens {VillageLivestock.HeartText(Save.loveHen)} · Rabbits {VillageLivestock.HeartText(Save.loveRabbit)}"), new Color(0.95f, 0.75f, 0.55f), (Action)null));
            if (Save.loveHorse > 0) rows.Add((Loc.T($" 말 {VillageLivestock.HeartText(Save.loveHorse)}", $" Horse {VillageLivestock.HeartText(Save.loveHorse)}"), new Color(0.80f, 0.62f, 0.45f), (Action)null));
            int b = BirthdayNpc(Save.week + 1); if (b >= 0) rows.Add((Loc.T($"♥ 다음 주는 {VillageDex.Npcs[b].ko} 생일 (좋아하는 것: {VillageDex.Npcs[b].likesKo})", $"♥ Next week: {VillageDex.Npcs[b].en}'s birthday"), new Color(0.98f, 0.70f, 0.80f), (Action)null));
            bool done = false;
            rows.Add((Loc.T("● 잘 자 — 내일 또", "● Good night"), new Color(0.60f, 0.52f, 0.92f), () => done = true));
            _hud.Choice(Loc.T($"■ {Save.week}주차 — 오늘의 기록", $"■ Week {Save.week} — today"), "", rows.ToArray());
            float t = 0f;
            while (!done && _hud.PopupOpen && t < 120f) { t += Time.unscaledDeltaTime; yield return null; }
            if (_hud.PopupOpen) _hud.ClosePopup();
        }

        // ── ④ 주민 하트 이벤트 · 생일 · 축제 대회 ─────────────────────────
        /// 생일 주차: 좋아하는 계절(season) 안에서 사람마다 다르게
        public static int BirthdayWeek(int i) { var n = VillageDex.Npcs[i]; return n.season * 13 + 1 + (i * 5 + 3) % 13; }
        public static int BirthdayNpc(int week) { for (int i = 0; i < VillageDex.Npcs.Length; i++) if (BirthdayWeek(i) == week) return i; return -1; }

        void ShowHeartEvent()
        {
            int i = VillageDex.PendingHeartNpc, lv = VillageDex.PendingHeartLv; VillageDex.PendingHeartNpc = -1;
            if (i < 0 || i >= VillageDex.Npcs.Length || lv < 1 || lv > 4) return;
            int bit = 1 << (lv - 1); if ((Save.npcHeartSeen[i] & bit) != 0) return;
            Save.npcHeartSeen[i] |= bit; _gm.Persist();
            string line = HeartLine(i, lv);
            if (string.IsNullOrEmpty(line)) return;
            _hud.Bubble(VillageDex.Npcs[i].ko + "  " + VillageDex.Hearts(VillageDex.Friend(Save, i)), line);
        }

        /// 호감 20·50·80·100 을 넘을 때 한 번씩 보는 짧은 장면(대사 한 마디 — 겪은 일·물건으로)
        static readonly string[][] HeartLines = {
            new[] { "숨비소리 들어 봤나? 휘— 하고 물 위로 올라올 때 내는 소리여.", "이거 가져가. 오늘 딴 소라여. 껍데기는 귀에 대 봐.", "테왁 줄 매듭은 이렇게 묶는 거여. 한 번 해 봐. …그렇지.", "물 들어가기 전에 등대 한 번, 너네 집 지붕 한 번 보고 들어간다." },
            new[] { "등대 불은 12초에 한 번 돌아. 세어 봐. 하나, 둘…", "계단이 백열두 칸이야. 올라올래? 꼭대기 창은 네 키에 딱 맞아.", "태풍 오던 밤에도 저 불은 켜 뒀어. 그날 배 한 척이 저 불 보고 들어왔지.", "내 쌍안경이야. 빨간 지붕 보이지? 저기 불 꺼지면 나도 불 끈다." },
            new[] { "유채꽃은 꺾으면 금방 고개를 숙여. 그래서 밭에서 봐야 해.", "말린 수국이야. 책 사이에 끼워 둬. 겨울에 펴면 여름 냄새 나.", "가게 열쇠가 하나 더 있어. 비 오는 날 화분 들이는 것 좀 도와줘.", "씨앗 봉투에 네 이름 적을 칸 비워 뒀어. 네 글씨로 써 줘." },
            new[] { "미끼는 새우가 최고야. 비밀이다?", "누나, 찌가 두 번 까딱하면 그때 당겨. 한 번은 속임수야.", "어제 옥돔 잡았어! 사진 봐 봐, 내 팔만 해.", "이 낚싯대 아빠가 준 거야. 누나 오늘 하루만 써." },
            new[] { "라떼 하트 성공! …아니다, 또 감자다.", "메뉴에 없는 거 하나 해 줄게요. 귤 크림 라떼. 사장님한텐 비밀.", "마감하고 남은 빵이에요. 봉투 두 개. 하나는 내일 아침 거.", "이 머그 손잡이에 테이프 붙여 뒀어요. 네 이름 써 있어요." },
            new[] { "파도는 일곱 번째가 제일 커. 세어 봐.", "보드 왁스는 이렇게, 작은 원을 그리면서 발라.", "새벽 여섯 시 바다가 제일 조용해. 내일 나와 볼래?", "내 첫 보드야. 모서리 깨진 거. 파도 좋은 날 네가 타." },
            new[] { "토마토 좋아하는 거 어떻게 알았어? 덤으로 사탕 하나.", "외상은 안 돼. …근데 너는 다음에 줘.", "창고 정리 좀 도와줄래? 끝나면 라면 끓여 줄게.", "문 닫는 날에도 뒷문은 열어 둘게. 두 번 두드려." },
            new[] { "지각 한 번도 안 했더라. 장부에 적어 뒀다.", "추천서 칭찬 칸이 모자라서 뒷장에 이어 썼다.", "내 첫 월급봉투야. 아직 안 뜯었어.", "일 없어도 들러. 커피 두 잔 타 둘게." },
            new[] { "무릎 까진 데 밴드 붙여 줄게요. 캐릭터 밴드밖에 없어요.", "바다 10초, 약속했죠? 새끼손가락.", "야간 끝나고 먹는 컵라면이에요. 하나 드실래요?", "비상 연락처 칸에 제 번호 적었어요. 밤에도 받아요." },
            new[] { "팬케이크 귤은 아침 여섯 시에 따. 그래야 새콤해.", "새 메뉴 시식해 볼래? 한라봉 버터.", "주방 들어와. 반죽 저어 봐. …빠르다, 빨라.", "메뉴판 맨 아래 칸에 네 이름 붙은 거 하나 넣었어." },
            new[] { "통장 정리해 드릴게요. 한 줄씩 찍히는 소리 좋죠?", "저금통 하나 드릴게요. 돼지 말고 귤 모양이에요.", "점심에 김밥 한 줄 나눠 드릴까요? 창구 뒤에서요.", "첫 통장 만들던 날 서명, 아직 보관하고 있어요. 삐뚤빼뚤한 거." },
            new[] { "그 후드, 소매 한 번만 접어 봐요. 그렇지.", "새로 들어온 옷, 택 떼기 전에 제일 먼저 보여 줄게요.", "피팅룸 거울 앞에서 한 바퀴만 돌아 봐요. …한 바퀴 더.", "수첩 첫 장에 당신 치수 적어 뒀어요." },
            new[] { "3번 트랙, 2분 11초에 기타 소리 들어 봐.", "헤드폰 써 봐. 오른쪽이 조금 커. 일부러 그런 거야.", "이 LP 가게에 한 장뿐이야. 네가 먼저 들어.", "새벽 방송 마지막 곡, 네가 흥얼거리던 거야." },
            new[] { "곱빼기는 그냥 줄게. 말하지 마.", "국물 간 좀 봐 줘. 짜? 싱거워?", "새벽 네 시에 뼈 고는 거 보러 올래?", "네 그릇은 따로 있어. 파란 줄 두 개." },
            new[] { "근고기는 이렇게 세워서 굽는 거야. 봐.", "멜젓 찍어 봐. 처음엔 다들 인상 써.", "불판 닦는 거 도와주면 다음엔 목살 서비스.", "단골 명단 첫 줄에 네 이름 적었다." },
            new[] { "포인트 적립 되세요? …없으면 지금 만들어 드릴게요.", "과자 신상 나왔어요. 계산 전에 하나 까 봐요.", "마감 세일 스티커 붙이기 전에 문자 드릴게요.", "계산대 옆에 의자 하나 뒀어요. 기다릴 때 앉아요." },
            new[] { "암모나이트 무늬 세어 봐요. 방이 서른 개예요.", "수장고 들어가 볼래요? 흰 장갑 끼고요.", "이 화석 라벨 발견자 칸에 당신 이름 넣어도 돼요?", "빈 진열장 하나는 그대로 뒀어요. 당신이 채울 자리." },
            new[] { "송아지는 귀 뒤를 긁어 주면 가만히 있어.", "우유 한 병 마셔 봐. 아침에 짠 거야.", "경매 날 같이 갈래? 손 번쩍 들면 진짜 사게 된다.", "제일 작은 송아지 이름 하나 지어 줘." },
            new[] { "하르방 코 만져 봤어요? 반들반들하죠.", "포장 리본 색 골라요. 빨강, 초록, 귤색.", "몰래 하나 만들었어요. 당신 닮은 하르방.", "가게 앞에서 사진 한 장 찍어요. 계산대 옆에 붙일 거예요." },
            new[] { "곡괭이는 힘으로 안 돼. 소리를 들어.", "헬멧 등, 내 거랑 똑같은 거다. 써 봐.", "갱도 끝 돌에 내 이름 새겨 뒀다. 옆자리 비워 뒀어.", "첫 금 캔 날 낀 장갑이다. 손가락 구멍 난 거. 가져가." },
        };
        static string HeartLine(int i, int lv) => i < HeartLines.Length ? HeartLines[i][Mathf.Clamp(lv - 1, 0, 3)] : "";

        // 축제 대회
        static string ContestName(int id) => id == 0 ? "유채꽃 작물 품평회" : id == 1 ? "여름밤 낚시 대회" : id == 2 ? "감귤 따기 대회" : "눈꽃 보석 품평회";
        static string ContestNameEn(int id) => id == 0 ? "Crop Fair" : id == 1 ? "Fishing Derby" : id == 2 ? "Tangerine Contest" : "Gem Fair";
        static int PriceOf(string id) { foreach (var (k, p) in VillageSell.Prices) if (k == id) return p; return 0; }

        /// 내 점수와 출품작 설명
        int ContestScore(int id, out string entry)
        {
            entry = ""; int best = 0; string bestId = null; int kinds = 0, count = 0;
            foreach (var (k, p) in VillageSell.Prices)
            {
                bool fit = id == 0 ? (k.StartsWith("crop_") || k.StartsWith("flower_")) : id == 1 ? (k.StartsWith("fish_") || k == "ing_fish") : id == 2 ? (k == "fruit_tangerine" || k == "fruit_hallabong") : (k.StartsWith("gem_") || k.StartsWith("ore_"));
                if (!fit || k == "fish_5") continue; int n = LifeItems.Count(Save, k); if (n <= 0) continue;
                kinds++; count += n; if (p > best) { best = p; bestId = k; }
            }
            if (bestId == null) return -1;
            var d = LifeItems.Get(bestId); string nm = d.HasValue ? LifeItems.Name(d.Value) : bestId;
            int year = Save.week / Timeline.Weeks; int star = Save.farmStarYear == year ? Save.farmBestStar : 0;
            int score = id == 0 ? best / 3 + star * 20 + kinds * 4 : id == 1 ? best / 2 + count * 3 : id == 2 ? LifeItems.Count(Save, "fruit_tangerine") * 6 + LifeItems.Count(Save, "fruit_hallabong") * 15 : best / 4 + kinds * 6;
            entry = id == 0 ? Loc.T($"{nm}{(star > 0 ? " " + new string('★', star) : "")} 외 {kinds}종", nm) : id == 2 ? Loc.T($"귤 {LifeItems.Count(Save, "fruit_tangerine")} · 한라봉 {LifeItems.Count(Save, "fruit_hallabong")}", "Tangerines") : Loc.T($"{nm} 외 {count - 1}", nm);
            return score + UnityEngine.Random.Range(0, 11);
        }

        void ContestMenu(VillageSeason.Ev ev)
        {
            if (Save == null) return;
            int bit = 1 << ev.id;
            if ((Save.contestMask & bit) != 0) { _hud.Bubble(Loc.T("대회 진행 요원", "Staff"), Loc.T("올해 대회는 벌써 나갔잖아요. 내년에 또 봐요!", "You already entered this year. See you next year!")); return; }
            string how = ev.id == 0 ? Loc.T("가방에서 제일 비싼 작물·꽃 + 올해 최고 품질 ★ + 가짓수로 겨룬다.", "Best crop + best ★ this year + variety.")
                        : ev.id == 1 ? Loc.T("가방에서 제일 귀한 물고기 + 마릿수로 겨룬다.", "Rarest fish + count.")
                        : ev.id == 2 ? Loc.T("가방의 귤·한라봉 개수로 겨룬다(과수원에서 따 오자).", "Count of tangerines & hallabong.")
                        : Loc.T("가방에서 제일 귀한 광석·보석 + 가짓수로 겨룬다(광산).", "Best gem/ore + variety (mine).");
            int pre = ContestScore(ev.id, out var entry);
            _hud.Choice(Loc.T($"★ {ContestName(ev.id)}", $"★ {ContestNameEn(ev.id)}"), how + Loc.T(" 한 해에 한 번만 나갈 수 있다.", " Once a year."),
                new (string, Color, Action)[] {
                    (pre < 0 ? Loc.T("출품할 게 가방에 없다", "Nothing to enter") : Loc.T($"출품하기 — {entry}", $"Enter — {entry}"), pre < 0 ? new Color(0.62f, 0.62f, 0.68f) : new Color(0.95f, 0.72f, 0.30f), pre < 0 ? (Action)null : () => RunContest(ev)),
                    (Loc.T("다음에", "Later"), new Color(0.62f, 0.62f, 0.68f), null),
                });
        }

        void RunContest(VillageSeason.Ev ev)
        {
            int mine = ContestScore(ev.id, out var entry); if (mine < 0) return;
            Save.contestMask |= 1 << ev.id;
            // 라이벌 셋(마을 사람) — 점수대는 대회마다 비슷하게
            var rng = new System.Random(Save.week * 31 + ev.id * 7 + Save.seed);
            var pool = new List<int>(); for (int i = 0; i < VillageDex.Npcs.Length; i++) pool.Add(i);
            var board = new List<(string who, int score, bool me)> { (Loc.T("나", "Me"), mine, true) };
            // 205-2(다듬기): 라이벌이 약했다(★★★ 수박이면 150점 쉽게 1등) — 1위 라이벌 110~190 · 2위 80~150 · 3위 55~115, 해가 갈수록 +10
            int yr = Save.week / Timeline.Weeks * 10; int[] lo = { 110 + yr, 80 + yr, 55 + yr }, hi = { 190 + yr, 150 + yr, 115 + yr };
            for (int k = 0; k < 3; k++) { int j = rng.Next(pool.Count); int ni = pool[j]; pool.RemoveAt(j); board.Add((VillageDex.Npcs[ni].ko, rng.Next(lo[k], hi[k]), false)); }
            board.Sort((a, b) => b.score.CompareTo(a.score));
            int rank = board.FindIndex(x => x.me) + 1;
            int money = rank == 1 ? 1000 : rank == 2 ? 500 : rank == 3 ? 250 : 50, shards = rank == 1 ? 5 : rank == 2 ? 3 : rank == 3 ? 1 : 0;
            Save.stats.money += money; Save.starShards += shards; Save.starShardsTotal += shards;
            if (rank == 1) LifeItems.Add(Save, "trophy_ribbon", 1);
            _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            if (VillageSeason.Booth != null) VillagePang.Burst(VillageSeason.Booth.position + Vector3.up * 3f, rank == 1 ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.6f, 0.75f), Color.white, 2.4f);
            var rows = new List<(string, Color, Action)>();
            for (int k = 0; k < board.Count; k++)
            {
                var r = board[k]; string medal = k < 3 ? "★" : "·";
                rows.Add(($"{medal} {k + 1}등  {r.who}  {r.score}점", r.me ? new Color(0.98f, 0.60f, 0.72f) : new Color(0.78f, 0.74f, 0.70f), (Action)null));
            }
            rows.Add((Loc.T($"상품: {money:N0}G{(shards > 0 ? $" · 별조각 +{shards}" : "")}{(rank == 1 ? " · 우승 리본" : "")}", $"Prize: {money:N0}G"), new Color(0.95f, 0.72f, 0.30f), (Action)null));
            _hud.Choice(Loc.T($"★ {ContestName(ev.id)} — {rank}등!", $"★ {ContestNameEn(ev.id)} — #{rank}!"), Loc.T($"출품: {entry}", $"Entry: {entry}"), rows.ToArray());
        }

        // ── 개발용 ───────────────────────────────────────────────────────
        public void DevDaySummary() { StartCoroutine(DaySummaryCo()); }
        public void DevCare(int kind) { if (kind == 2) RanchMenu(); else { if (kind == 0 && Save.farmChickens == 0) VillageLivestock.AddChicken(Save); if (kind == 1 && VillageLivestock.Rabbits(Save) == 0) VillageLivestock.AddRabbit(Save); CareMenu(kind); } }
        public void DevLove(int amt) { Save.loveHen = Mathf.Clamp(Save.loveHen + amt, 0, 100); Save.loveRabbit = Mathf.Clamp(Save.loveRabbit + amt, 0, 100); Save.loveHorse = Mathf.Clamp(Save.loveHorse + amt, 0, 100); RefreshStatus(); Debug.LogWarning($"[205] love hen={Save.loveHen} rabbit={Save.loveRabbit} horse={Save.loveHorse}"); }
        public void DevRide() { StartRide(); Debug.LogWarning($"[205] ride t={_rideT:F0} horse={(_rideHorse != null ? _rideHorse.position.ToString() : "null")} player={_player.position} rig={(_rigT != null ? _rigT.localPosition.ToString() : "-")}"); }
        public void DevGarden() { GardenMenu(); }
        public void DevHeart(int npc, int lv) { VillageDex.PendingHeartNpc = npc; VillageDex.PendingHeartLv = lv; if (Save.npcHeartSeen != null && npc < Save.npcHeartSeen.Length) Save.npcHeartSeen[npc] &= ~(1 << (lv - 1)); }
        public void DevGoWestGarden() { var c = VillageFarm.TileCenter(10); Teleport(VillageWorld.Ground(c.x, c.z + 0.2f), 0f); }
        public void DevRipen() { VillageFarm.Ensure(Save); for (int i = 0; i < VillageFarm.Tiles; i++) { var sd = VillageFarm.SeedOf(Save, i); if (sd != null) Save.farm[i].growth = sd.weeks; } VillageWorld.BuildCrops(_world, Save); DevFarmLog(); }
        public int DevTile; public void DevHarvestHere() { if (_hud.Locked) _hud.ClosePopup(); var tc = VillageFarm.TileCenter(DevTile); Teleport(VillageWorld.Ground(tc.x, tc.z), 180f); FarmAct(); var sb = new System.Text.StringBuilder("[205] bag "); foreach (var id in new[] { "crop_strawberry", "crop_tomato", "crop_potato", "ing_rice", "crop_rice", "flower_rose" }) sb.Append(id).Append('=').Append(LifeItems.Count(Save, id)).Append(' '); sb.Append("last★=").Append(VillageFarm.LastStar); Debug.LogWarning(sb.ToString()); }
        public void DevGoMineMouth() { Teleport(VillageWorld.Ground(VillageZones.MineMouth.x, VillageZones.MineMouth.y - 8f), 0f); _camYaw = _camYawTarget = 0f; SnapCamera(); }   // 206차: 마을 광산 입구를 정면에서 보기
        public void DevMoney() { Save.stats.money += 10000; RefreshStatus(); }
        public void DevGiveContestItems() { LifeItems.Add(Save, "crop_watermelon", 2); LifeItems.Add(Save, "fish_3", 1); LifeItems.Add(Save, "fruit_tangerine", 8); LifeItems.Add(Save, "gem_amethyst", 1); Save.farmBestStar = 3; Save.farmStarYear = Save.week / Timeline.Weeks; }
        public void DevContest(int id) { Save.contestMask &= ~(1 << id); ContestMenu(VillageSeason.All[Mathf.Clamp(id, 0, 3)]); }
        public void DevFarmLog()
        {
            VillageFarm.Ensure(Save); var sb = new System.Text.StringBuilder($"[205] farm rows={VillageFarm.Rows} tiles={VillageFarm.Tiles} best★={Save.farmBestStar} ");
            for (int i = 0; i < VillageFarm.Tiles; i++) { var sd = VillageFarm.SeedOf(Save, i); sb.Append($"[{i}:{(sd != null ? sd.id : "-")} g{Save.farm[i].growth} f{Save.farmFert[i]} m{Save.farmMiss[i]} ★{VillageFarm.Star(Save, i)}] "); }
            Debug.LogWarning(sb.ToString());
        }
        /// 밭 북쪽(늘어날 줄) 자리에 뭐가 있는지 — 렌더러 이름을 찍는다
        public void DevFarmProbe()
        {
            float gx = VillageWorld.GardenX, gz = VillageWorld.GardenZ;
            Probe("north", gx - 4.2f, gx + 4.2f, gz + 3.6f, gz + 3.6f + 5f);
            Probe("west", gx - 4.4f - 8.4f, gx - 4.4f, gz - 3.7f, gz + 3.7f);
            Probe("east", gx + 4.4f, gx + 4.4f + 8.4f, gz - 3.7f, gz + 3.7f);
            Probe("south", gx - 4.2f, gx + 4.2f, gz - 3.8f - 5f, gz - 3.8f);
        }
        void Probe(string tag, float x0, float x1, float z0, float z1)
        {
            var hits = new Dictionary<string, int>();
            foreach (var r in _world.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds; if (b.max.x < x0 || b.min.x > x1 || b.max.z < z0 || b.min.z > z1) continue;
                if (b.size.x > 40f || b.size.z > 40f) continue;
                var t = r.transform; string nm = t.name; while (t.parent != null && t.parent != _world) { t = t.parent; nm = t.name; }
                hits[nm] = hits.TryGetValue(nm, out var c) ? c + 1 : 1;
            }
            var sb = new System.Text.StringBuilder($"[205] probe {tag} x{x0:F1}~{x1:F1} z{z0:F1}~{z1:F1}: ");
            foreach (var kv in hits) sb.Append(kv.Key).Append('×').Append(kv.Value).Append(' ');
            Debug.LogWarning(sb.ToString());
        }
    }
}
