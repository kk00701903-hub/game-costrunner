using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 194차(사용자): 동쪽 새 동네 — 은행(예금+이자 · 대출 · K-POP 러닝 수입 정산), 제주 브런치 카페(먹고 HP 회복), 버스 정류장.
    public partial class VillageHub
    {
        public const float DepositRate = 0.015f;  // 예금 주 1.5 % (195차 밸런스: 2 → 1.5)
        public const float LoanRate = 0.04f;      // 대출 주 4 %
        public const int LoanLimit = 3000;
        public const float SettleBonus = 0.05f;   // 러닝 수입 정산 보너스 5 %

        void AddEastSpots()
        {
            if (VillageEast.BusStop != null)
                _spots.Add(new Spot { id = "bus", title = Loc.T("🚌 버스 정류장 · 시간표 보기", "🚌 Bus stop · Timetable"), pos = VillageWorld.Ground(VillageEast.StopX - 0.6f, VillageEast.StopZ), radius = 2.6f, on = BusStopTalk });
        }

        void EnterBank()
        {
            if (_busy || _interior != null || VillageZones.CityBank == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("은행", "Bank"), Loc.T("문이 닫혔다. 영업은 오전 8시 ~ 오후 7시.", "Closed. Open 8 AM – 7 PM.")); return; }
            EnterHouse(VillageZones.CityBank, Loc.T("은행", "Bank"), DoorOf(VillageZones.CityBank));   // 195차: 은행은 시내
        }
        void EnterCafe()
        {
            if (_busy || _interior != null || VillageEast.Cafe == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("귤빛 브런치", "Tangerine Brunch"), Loc.T("오늘 영업 끝! 브런치는 오전 8시 ~ 오후 7시.", "Closed for today. Open 8 AM – 7 PM.")); return; }
            EnterHouse(VillageEast.Cafe, Loc.T("귤빛 브런치", "Tangerine Brunch"), DoorOf(VillageEast.Cafe));
        }
        static Vector3 DoorOf(Transform house) { foreach (var hs in VillageWorld.Houses) if (hs.house == house) return hs.door; return house != null ? house.position : Vector3.zero; }

        /// 실내 종류(EnterHouse 에서) — 문으로 걸어 들어가도 상점·병원·은행·카페 방이 제대로 꾸며지게.
        static string InteriorKindOf(Transform house)
        {
            if (house == null) return null;
            if (house == VillageWorld.Shop) return "shop";
            if (house == VillageWorld.JobHouse) return "job";
            if (house == VillageWorld.Hospital) return "hospital";
            if (house == VillageZones.CityBank) return "bank";   // 195차
            if (house == VillageZones.Boutique) return "boutique"; if (house == VillageZones.Records) return "records"; if (house == VillageZones.Noodle) return "noodle";
            if (house == VillageZones.Pork) return "pork"; if (house == VillageZones.Mart) return "mart"; if (house == VillageZones.Museum) return "museum";
            if (house == VillageZones.Market) return "market"; if (house == VillageZones.Souvenir) return "souv";
            if (house == VillageZones.Teddy) return "teddy";   // 196차
            if (house == VillageZones.Gacha) return "gacha"; if (house == VillageZones.Workshop) return "workshop";   // 198차
            if (house == VillageEast.Cafe) return "cafe";
            return null;
        }

        // ── 공통: 실내 가구 끄기 · 상자 · NPC ──
        Transform RoomHost(string name)
        {
            var host = new GameObject(name).transform; host.SetParent(_interior.transform, false);
            foreach (Transform ch in _interior.transform)
            {
                string n = ch.name;
                if (n == "Plank" || n == "Bed" || n == "Mattress" || n == "Pillow" || n == "Blanket" || n == "Headboard" || n == "Skirt" || n == "Table" || n == "Chair" || n == "ChairBack" || n == "Leg" || n == "Shelf" || n == "Book" || n == "Pot" || n == "Plant" || n == "Cup" || n == "RugIn" || n == "LampBase" || n == "LampShade" || n == "Curtain") ch.gameObject.SetActive(false);
            }
            return host;
        }
        /// 211차(사용자: 「보강해줘」 — 가게 실내 소품이 상자): 이름표로 상자를 블렌더 소품(village_shop_kit.py)으로 바꾼다.
        /// 모델은 실제 크기로 만들었으므로 배율 1 로 상자 바닥 가운데에 세우고, 상자는 충돌만 남긴다(렌더러 끔). 받침만 떠 있는 판(탁자 윗판·선반 판)은 바닥에 세운다.
        void DressRoom(Transform host, float fy)
        {
            var kids = new List<Transform>(); foreach (Transform ch in host) kids.Add(ch);
            void Hide(Transform t) { var r = t.GetComponent<MeshRenderer>(); if (r != null) r.enabled = false; }
            GameObject Put(string model, Vector3 at, float yaw) { var g = JejuKit.Spawn(model, host, Vector3.zero, yaw, 1f); if (g != null) g.transform.position = at; return g; }
            GameObject Swap(Transform t, string model, float yaw, bool onFloor = false)
            {
                var r = t.GetComponent<MeshRenderer>(); if (r == null || !r.enabled) return null; var b = r.bounds;
                var g = Put(model, new Vector3(b.center.x, onFloor ? fy : b.min.y, b.center.z), yaw); if (g != null) Hide(t); return g;
            }
            bool shelvesDone = false;
            foreach (var t in kids)
            {
                switch (t.name)
                {
                    case "ShopCounter": Swap(t, "VShopCounter", 180f); break;
                    case "Table": Swap(t, "VDiningTable", 0f, true); break;
                    case "Grill": break;
                    case "Kitchen": Swap(t, "VKitchen", 180f); break;
                    case "Pedestal": Swap(t, "VPedestal", 180f); break;
                    case "TeddyCase": { var g = Swap(t, "VDisplayCase", 180f); if (g != null) Put("VTeddy", new Vector3(t.position.x, fy + 0.92f, t.position.z), 180f); break; }
                    case "TeddyB": case "TeddyH": case "GachaDome": case "Capsule": case "AnvilBase": case "ForgeFire": case "Goods": Hide(t); break;
                    case "GachaBase": Swap(t, "VGacha", 180f); break;
                    case "Anvil": Swap(t, "VAnvil", 180f, true); break;
                    case "Forge": Swap(t, "VForge", 180f); break;
                    case "HayBale": Swap(t, "VHayBale", 0f); break;
                    case "Pen": Swap(t, "VPenFence", 0f); break;
                    case "MartShelf":
                    case "SouvShelf":
                        Hide(t);
                        if (!shelvesDone)
                        {
                            shelvesDone = true; bool mart = t.name == "MartShelf";
                            float z = mart ? t.position.z : VillageInterior.OZ + VillageInterior.RD * 0.5f - 0.5f;
                            for (int k = 0; k < 3; k++) Put("VShelfUnit", new Vector3(VillageInterior.OX - 3f + k * 3f, fy, z), 180f);
                        }
                        break;
                }
            }
        }

        GameObject RB(Transform host, string n, Vector3 pos, Vector3 size, Color col, bool collide = false)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); if (!collide) Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(host, false);
            g.transform.position = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(col, 0.1f); return g;
        }
        Transform RoomNpc(Transform host, string model, Vector3 pos, float yaw, string line)
        {
            var npcRoot = new GameObject("Clerk").transform; npcRoot.SetParent(host, false); npcRoot.position = pos; npcRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(npcRoot, false);
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/" + model, pivot, 1.0f, true);
            if (rig != null)
            {
                var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, 0.12f); anim.speed = 0f; }
                var mo = pivot.gameObject.AddComponent<CharacterMotion>(); mo.Anim = anim; mo.LookTarget = _player; mo.FootDust = false;
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                StartCoroutine(ClerkFace(anim, rig.gameObject, model, npcRoot));
            }
            var bubble = SpeechBubble.Create(npcRoot, 1.6f); bubble.Show(line);
            return npcRoot;
        }

        /// 바닥·벽 색 + 벽을 3 m 더 올린다(낮은 벽 너머로 하늘이 보이던 것 — 병원과 같은 처리)
        void TintRoom(Color floor, Color wall, Color skirt, string floorTex = null, string wallTex = null)
        {
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW, wh = VillageInterior.WallH;
            // 195차(사용자: 「파이어플라이 텍스처」): 그림이 있으면 바닥·벽에 Firefly 타일
            Material TexOr(string tex, Vector2 tiling, Color fb, float sm0) { var t = tex != null ? Resources.Load<Texture2D>("CoastRun/Textures/Village/" + tex) : null; if (t == null) return CoastMaterials.CreateLit(fb, sm0); var m = CoastMaterials.CreateToon(new Color(0.98f, 0.98f, 0.98f), t, 0.05f); m.mainTextureScale = tiling; return m; }
            var fm = TexOr(floorTex, new Vector2(4f, 3f), floor, 0.1f); var wm = TexOr(wallTex, new Vector2(3f, 1.2f), wall, 0.05f); var sm = CoastMaterials.CreateLit(skirt, 0.1f);
            foreach (Transform ch in _interior.transform)
            {
                var r = ch.GetComponent<MeshRenderer>(); if (r == null) continue;
                if (ch.name == "Floor") r.sharedMaterial = fm; else if (ch.name == "WallN" || ch.name == "WallW" || ch.name == "WallE") r.sharedMaterial = wm; else if (ch.name == "Skirt") r.sharedMaterial = sm;
            }
            var host = _interior.transform;
            RB(host, "UpWallN", new Vector3(ox, fy + wh + 1.5f, oz + rd * 0.5f), new Vector3(rw, 3f, 0.2f), wall).GetComponent<MeshRenderer>().sharedMaterial = wm;
            RB(host, "UpWallW", new Vector3(ox - rw * 0.5f, fy + wh + 1.5f, oz), new Vector3(0.2f, 3f, rd), wall).GetComponent<MeshRenderer>().sharedMaterial = wm;
            RB(host, "UpWallE", new Vector3(ox + rw * 0.5f, fy + wh + 1.5f, oz), new Vector3(0.2f, 3f, rd), wall).GetComponent<MeshRenderer>().sharedMaterial = wm;
            RB(host, "WallStripe", new Vector3(ox, fy + 0.9f, oz + rd * 0.5f - 0.11f), new Vector3(rw, 0.14f, 0.03f), skirt);
        }

        // ── 은행 ────────────────────────────────────────────────────────
        void BuildBankRoom()
        {
            if (_interior == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW;
            var host = RoomHost("BankRoom");
            var wood = new Color(0.55f, 0.38f, 0.26f); var marble = new Color(0.93f, 0.92f, 0.88f); var gold = new Color(1f, 0.80f, 0.28f); var green = new Color(0.30f, 0.52f, 0.42f);
            // 창구 카운터(유리 칸막이 3칸) + 금고
            RB(host, "BankCounter", new Vector3(ox, fy + 0.4f, oz + 1.4f), new Vector3(6.4f, 0.8f, 0.8f), wood, true);
            RB(host, "BankCounterTop", new Vector3(ox, fy + 0.82f, oz + 1.4f), new Vector3(6.6f, 0.06f, 0.95f), marble);
            // 창구 칸막이 — 금색 기둥 + 윗 가로대만(유리판은 불투명하게 보여 은행원을 가렸다)
            for (int i = -3; i <= 3; i += 2) RB(host, "BankPost", new Vector3(ox + i * 1.05f, fy + 1.3f, oz + 1.45f), new Vector3(0.06f, 0.9f, 0.06f), gold);
            RB(host, "BankRailTop", new Vector3(ox, fy + 1.86f, oz + 1.45f), new Vector3(6.4f, 0.06f, 0.06f), gold);
            for (int i = -1; i <= 1; i++) RB(host, "WindowNo", new Vector3(ox + i * 2.1f, fy + 2.02f, oz + 1.45f), new Vector3(0.36f, 0.26f, 0.03f), new Color(0.20f, 0.30f, 0.45f));
            TintRoom(new Color(0.86f, 0.80f, 0.70f), new Color(0.80f, 0.90f, 0.86f), new Color(0.40f, 0.58f, 0.50f), "Tex_BankFloor", "Tex_BankWall");
            RB(host, "Vault", new Vector3(ox + rw * 0.5f - 0.6f, fy + 1.2f, oz + rd * 0.5f - 0.6f), new Vector3(1.1f, 2.4f, 0.9f), new Color(0.52f, 0.56f, 0.62f), true);
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(wheel.GetComponent<Collider>()); wheel.name = "VaultWheel"; wheel.transform.SetParent(host, false);
            wheel.transform.position = new Vector3(ox + rw * 0.5f - 1.17f, fy + 1.3f, oz + rd * 0.5f - 0.6f); wheel.transform.rotation = Quaternion.Euler(0f, 0f, 90f); wheel.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);
            wheel.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(gold, 0.5f);
            // 대기 의자 · 화분 · 금화 간판 · 초록 카펫
            RB(host, "BankRug", new Vector3(ox, fy + 0.02f, oz - 1.2f), new Vector3(4.2f, 0.02f, 2.6f), green);
            for (int i = 0; i < 3; i++) RB(host, "WaitBench", new Vector3(ox - rw * 0.5f + 0.7f, fy + 0.3f, oz - 2.2f + i * 1.3f), new Vector3(0.6f, 0.08f, 1.0f), wood);
            RB(host, "CoinBoard", new Vector3(ox, fy + 2.55f, oz + rd * 0.5f - 0.12f), new Vector3(2.6f, 0.7f, 0.05f), new Color(0.20f, 0.30f, 0.45f));
            RB(host, "CoinBoardGold", new Vector3(ox, fy + 2.55f, oz + rd * 0.5f - 0.15f), new Vector3(2.2f, 0.12f, 0.03f), gold);
            RoomNpc(host, "Npc_Keeper", new Vector3(ox, fy, oz + 2.4f), 180f, Loc.T("은행원: 어서 오세요! 예금·대출·러닝 수입 정산 모두 여기서.", "Teller: Welcome! Deposits, loans and run income here."));
            _spots.Add(new Spot { id = "in_bank", title = Loc.T("🏦 은행 창구 · 예금 / 대출 / 러닝 수입 정산", "🏦 Teller · Deposit / Loan / Run income"), pos = new Vector3(ox, fy, oz + 0.4f), radius = 2.0f, on = BankMenu });
            BuildingOutline.Attach(host, 0.018f);
            int gained = ApplyBankInterest(out int loanGrew);
            if (gained > 0 || loanGrew > 0)
                CoastToast.Show(Loc.T($"🏦 이자 정산 — 예금 +{gained:N0}G" + (loanGrew > 0 ? $" · 대출 이자 +{loanGrew:N0}G" : ""), $"🏦 Interest — deposit +{gained:N0}G" + (loanGrew > 0 ? $" · loan +{loanGrew:N0}G" : "")));
        }

        static int WeekIndex(SaveData s) => s != null ? s.week : 0;
        /// 지난번 은행 방문 뒤 지난 주만큼 예금(+2 %)·대출(+4 %) 이자를 붙인다. 붙은 예금 이자를 돌려준다.
        int ApplyBankInterest(out int loanGrew)
        {
            loanGrew = 0; if (Save == null) return 0;
            int now = WeekIndex(Save);
            if (Save.bankWeekStamp < 0) { Save.bankWeekStamp = now; _gm.Persist(); return 0; }
            int weeks = now >= Save.bankWeekStamp ? now - Save.bankWeekStamp : now + 52 - Save.bankWeekStamp;
            Save.bankWeekStamp = now;
            if (weeks <= 0) return 0;
            int d0 = Save.bankDeposit, l0 = Save.bankLoan;
            if (d0 > 0) Save.bankDeposit = Mathf.Min(999999, Mathf.RoundToInt(d0 * Mathf.Pow(1f + DepositRate, weeks)));
            if (l0 > 0) Save.bankLoan = Mathf.RoundToInt(l0 * Mathf.Pow(1f + LoanRate, weeks));
            loanGrew = Save.bankLoan - l0; _gm.Persist();
            return Save.bankDeposit - d0;
        }

        string BankStatus() => Loc.T($"지갑 {Save.stats.money:N0}G  ·  예금 {Save.bankDeposit:N0}G (주 {DepositRate * 100:0}% 이자)  ·  대출 {Save.bankLoan:N0}G (주 {LoanRate * 100:0}%)",
                                    $"Wallet {Save.stats.money:N0}G · Deposit {Save.bankDeposit:N0}G ({DepositRate * 100:0}%/wk) · Loan {Save.bankLoan:N0}G ({LoanRate * 100:0}%/wk)");

        void BankMenu()
        {
            if (Save == null) return;
            VillageDex.Talk(Save, VillageDex.NpcIndex("teller"), 1);   // 195차
            var blue = new Color(0.35f, 0.58f, 0.90f); var green = new Color(0.40f, 0.75f, 0.50f); var orange = new Color(0.95f, 0.62f, 0.30f); var pink = new Color(0.95f, 0.55f, 0.70f); var grey = new Color(0.6f, 0.6f, 0.66f);
            string settle = Save.kpopUnsettled > 0
                ? Loc.T($"🎵 K-POP 러닝 수입 정산 ({Save.kpopUnsettledRuns}판 · {Save.kpopUnsettled:N0}G → 보너스 +{SettleBonusOf(Save.kpopUnsettled):N0}G)", $"🎵 Settle K-POP run income ({Save.kpopUnsettledRuns} runs · {Save.kpopUnsettled:N0}G → +{SettleBonusOf(Save.kpopUnsettled):N0}G)")
                : Loc.T("🎵 K-POP 러닝 수입 정산 (정산할 수입 없음)", "🎵 Settle K-POP run income (nothing yet)");
            _hud.Choice(Loc.T("🏦 은행 창구", "🏦 Bank teller"), BankStatus(), new (string, Color, Action)[] {
                (Loc.T("💰 예금하기", "💰 Deposit"), blue, () => AmountMenu(true)),
                (Loc.T("💵 예금 찾기", "💵 Withdraw"), green, () => AmountMenu(false)),
                (Loc.T($"🏦 대출받기 (한도 {LoanLimit:N0}G)", $"🏦 Take a loan (limit {LoanLimit:N0}G)"), orange, LoanMenu),
                (Loc.T("↩ 대출 갚기", "↩ Repay loan"), pink, RepayMenu),
                (settle, new Color(0.62f, 0.50f, 0.92f), SettleRunIncome),
                (Loc.T("나가기", "Leave"), grey, () => { }),
            });
        }
        static int SettleBonusOf(int income) => income <= 0 ? 0 : Mathf.Max(10, Mathf.RoundToInt(income * SettleBonus));

        void AmountMenu(bool deposit)
        {
            int have = deposit ? Save.stats.money : Save.bankDeposit;
            if (have <= 0) { _hud.Bubble(Loc.T("은행원", "Teller"), deposit ? Loc.T("지갑이 비어 있어요. 러닝으로 벌어 오세요!", "Your wallet is empty. Earn some on a run!") : Loc.T("찾을 예금이 없어요.", "No deposit to withdraw.")); return; }
            var list = new System.Collections.Generic.List<(string, Color, Action)>();
            foreach (int a in new[] { 100, 500, 1000, 5000 }) if (a < have) { int amt = a; list.Add(($"{amt:N0}G", new Color(0.45f, 0.65f, 0.90f), () => DoMove(deposit, amt))); }
            list.Add((Loc.T($"전부 ({have:N0}G)", $"All ({have:N0}G)"), new Color(0.35f, 0.55f, 0.85f), () => DoMove(deposit, have)));
            list.Add((Loc.T("취소", "Cancel"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(deposit ? Loc.T("💰 얼마를 맡길까요?", "💰 How much to deposit?") : Loc.T("💵 얼마를 찾을까요?", "💵 How much to withdraw?"), BankStatus(), list.ToArray());
        }
        void DoMove(bool deposit, int amt)
        {
            if (deposit) { amt = Mathf.Min(amt, Save.stats.money); Save.stats.money -= amt; Save.bankDeposit += amt; }
            else { amt = Mathf.Min(amt, Save.bankDeposit); Save.bankDeposit -= amt; Save.stats.money += amt; }
            if (Save.bankWeekStamp < 0) Save.bankWeekStamp = WeekIndex(Save);
            _gm.Persist(); RefreshStatus();
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            CoastToast.Show(deposit ? Loc.T($"💰 {amt:N0}G 예금 완료 — 한 주마다 {DepositRate * 100:0}% 이자가 붙어요", $"💰 Deposited {amt:N0}G — {DepositRate * 100:0}% interest a week") : Loc.T($"💵 {amt:N0}G 찾았어요", $"💵 Withdrew {amt:N0}G"));
        }
        void LoanMenu()
        {
            int room = LoanLimit - Save.bankLoan;
            if (room <= 0) { _hud.Bubble(Loc.T("은행원", "Teller"), Loc.T($"대출 한도 {LoanLimit:N0}G 를 다 쓰셨어요. 먼저 조금 갚아 주세요.", $"You've hit the {LoanLimit:N0}G limit. Repay some first.")); return; }
            var list = new System.Collections.Generic.List<(string, Color, Action)>();
            foreach (int a in new[] { 500, 1000, 2000, 3000 }) if (a <= room) { int amt = a; list.Add(($"{amt:N0}G", new Color(0.95f, 0.62f, 0.30f), () => { Save.bankLoan += amt; Save.stats.money += amt; if (Save.bankWeekStamp < 0) Save.bankWeekStamp = WeekIndex(Save); _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f); CoastToast.Show(Loc.T($"🏦 {amt:N0}G 대출 — 한 주마다 {LoanRate * 100:0}% 이자가 붙어요", $"🏦 Borrowed {amt:N0}G — {LoanRate * 100:0}% a week")); })); }
            list.Add((Loc.T("취소", "Cancel"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(Loc.T("🏦 얼마를 빌릴까요?", "🏦 How much to borrow?"), Loc.T($"남은 한도 {room:N0}G · 이자 주 {LoanRate * 100:0}% (예금 이자보다 비싸요!)", $"Room {room:N0}G · {LoanRate * 100:0}%/wk (costlier than deposit interest!)"), list.ToArray());
        }
        void RepayMenu()
        {
            if (Save.bankLoan <= 0) { _hud.Bubble(Loc.T("은행원", "Teller"), Loc.T("갚을 대출이 없어요. 훌륭해요!", "No loan to repay. Great!")); return; }
            int can = Mathf.Min(Save.bankLoan, Save.stats.money);
            if (can <= 0) { _hud.Bubble(Loc.T("은행원", "Teller"), Loc.T("지갑에 돈이 없어요. 예금을 찾거나 러닝으로 벌어 오세요.", "No cash on hand. Withdraw or earn on a run.")); return; }
            var list = new System.Collections.Generic.List<(string, Color, Action)>();
            if (can > 500) list.Add(("500G", new Color(0.95f, 0.55f, 0.70f), () => Repay(500)));
            list.Add((Loc.T($"갚을 수 있는 만큼 ({can:N0}G)", $"As much as I can ({can:N0}G)"), new Color(0.90f, 0.45f, 0.62f), () => Repay(can)));
            list.Add((Loc.T("취소", "Cancel"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(Loc.T("↩ 대출 갚기", "↩ Repay"), BankStatus(), list.ToArray());
        }
        void Repay(int amt)
        {
            amt = Mathf.Min(amt, Mathf.Min(Save.bankLoan, Save.stats.money)); if (amt <= 0) return;
            Save.stats.money -= amt; Save.bankLoan -= amt; _gm.Persist(); RefreshStatus();
            CoastToast.Show(Save.bankLoan <= 0 ? Loc.T("🎉 대출을 다 갚았어요!", "🎉 Loan paid off!") : Loc.T($"↩ {amt:N0}G 갚음 — 남은 대출 {Save.bankLoan:N0}G", $"↩ Repaid {amt:N0}G — {Save.bankLoan:N0}G left"));
        }
        void SettleRunIncome()
        {
            if (Save.kpopUnsettled <= 0)
            {
                _hud.Bubble(Loc.T("은행원", "Teller"), Loc.T("정산할 러닝 수입이 아직 없어요. K-POP 러닝에서 번 돈은 여기서 정산하면 5% 보너스를 드려요!", "No run income yet. Settle K-POP earnings here for a 5% bonus!"));
                return;
            }
            int income = Save.kpopUnsettled, runs = Save.kpopUnsettledRuns, bonus = SettleBonusOf(income);
            Save.stats.money += bonus; Save.kpopUnsettled = 0; Save.kpopUnsettledRuns = 0; _gm.Persist(); RefreshStatus();
            VillagePang.Burst(_player.position + Vector3.up * 1.0f, new Color(1f, 0.85f, 0.35f), Color.white, 1.1f);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.6f);
            _hud.Bubble(Loc.T("은행원", "Teller"), Loc.T($"K-POP 러닝 {runs}판 수입 {income:N0}G 정산 완료! 정산 보너스 +{bonus:N0}G 넣어 드렸어요.", $"Settled {runs} K-POP runs ({income:N0}G). Bonus +{bonus:N0}G added!"));
        }

        // ── 제주 브런치 카페 ─────────────────────────────────────────────
        struct BrunchItem { public string ko, en; public int price, hp, stress; public Color col; }
        static readonly BrunchItem[] Brunch = {
            new BrunchItem { ko = "🍊 한라봉 에이드", en = "🍊 Hallabong ade", price = 120, hp = 20, stress = 5, col = new Color(1f, 0.66f, 0.22f) },
            new BrunchItem { ko = "🥞 감귤 팬케이크", en = "🥞 Tangerine pancakes", price = 200, hp = 40, stress = 3, col = new Color(0.98f, 0.78f, 0.40f) },
            new BrunchItem { ko = "🥜 우도 땅콩 라떼", en = "🥜 Udo peanut latte", price = 180, hp = 25, stress = 10, col = new Color(0.80f, 0.62f, 0.42f) },
            new BrunchItem { ko = "🍳 흑돼지 에그 베네딕트", en = "🍳 Black-pork eggs Benedict", price = 380, hp = 90, stress = 4, col = new Color(0.92f, 0.52f, 0.40f) },
            new BrunchItem { ko = "🥗 해녀 전복 브런치 플레이트", en = "🥗 Haenyeo abalone plate", price = 600, hp = 200, stress = 8, col = new Color(0.45f, 0.72f, 0.80f) },
        };

        void BuildCafeRoom()
        {
            if (_interior == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW;
            var host = RoomHost("CafeRoom");
            TintRoom(new Color(0.84f, 0.66f, 0.46f), new Color(1f, 0.95f, 0.84f), new Color(0.98f, 0.62f, 0.25f), "Tex_CafeFloor", "Tex_CafeWall");
            var wood = new Color(0.78f, 0.60f, 0.40f); var woodD = new Color(0.55f, 0.40f, 0.26f); var orange = new Color(1f, 0.60f, 0.18f); var basalt = new Color(0.32f, 0.33f, 0.36f);
            // 현무암 바 카운터 + 나무 상판 · 에스프레소 머신 · 귤 바구니
            RB(host, "CafeBar", new Vector3(ox + 1.2f, fy + 0.38f, oz + 1.5f), new Vector3(4.2f, 0.76f, 0.8f), basalt, true);
            RB(host, "CafeBarTop", new Vector3(ox + 1.2f, fy + 0.79f, oz + 1.5f), new Vector3(4.4f, 0.07f, 0.95f), wood);
            RB(host, "Espresso", new Vector3(ox + 2.6f, fy + 1.07f, oz + 1.55f), new Vector3(0.6f, 0.5f, 0.45f), new Color(0.75f, 0.78f, 0.82f));
            RB(host, "OrangeBasket", new Vector3(ox + 0.1f, fy + 0.92f, oz + 1.5f), new Vector3(0.6f, 0.2f, 0.45f), woodD);
            for (int i = 0; i < 5; i++) { var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>()); o.name = "Tangerine"; o.transform.SetParent(host, false); o.transform.position = new Vector3(ox - 0.1f + (i % 3) * 0.2f, fy + 1.09f + (i / 3) * 0.1f, oz + 1.45f + (i % 2) * 0.1f); o.transform.localScale = Vector3.one * 0.18f; o.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(orange, 0.3f); }
            // 메뉴판(뒷벽) — 칠판 + 귤색 줄
            RB(host, "MenuBoard", new Vector3(ox + 1.2f, fy + 2.35f, oz + rd * 0.5f - 0.12f), new Vector3(2.6f, 1.2f, 0.05f), new Color(0.18f, 0.24f, 0.22f));
            for (int i = 0; i < 4; i++) RB(host, "MenuLine", new Vector3(ox + 1.2f, fy + 2.7f - i * 0.25f, oz + rd * 0.5f - 0.15f), new Vector3(1.9f - (i % 2) * 0.4f, 0.06f, 0.02f), i == 0 ? orange : new Color(0.95f, 0.95f, 0.90f));
            // 창밖 바다(남쪽 벽 대신 큰 창 — 하늘·바다색 판)
            RB(host, "SeaWindow", new Vector3(ox - rw * 0.5f + 0.12f, fy + 1.7f, oz), new Vector3(0.04f, 1.4f, 3.4f), new Color(0.55f, 0.82f, 0.95f));
            RB(host, "SeaWindowSea", new Vector3(ox - rw * 0.5f + 0.14f, fy + 1.25f, oz), new Vector3(0.03f, 0.5f, 3.4f), new Color(0.25f, 0.62f, 0.85f));
            RB(host, "SeaWindowFrame", new Vector3(ox - rw * 0.5f + 0.15f, fy + 1.7f, oz), new Vector3(0.03f, 1.45f, 0.06f), Color.white);
            // 테이블 2 + 의자 · 귤 화분
            for (int t = 0; t < 2; t++)
            {
                float tx = ox - 2.6f, tz = oz - 1.8f + t * 2.4f;
                RB(host, "CafeTable", new Vector3(tx, fy + 0.72f, tz), new Vector3(1.0f, 0.06f, 1.0f), wood, true);
                RB(host, "CafeTableLeg", new Vector3(tx, fy + 0.36f, tz), new Vector3(0.1f, 0.72f, 0.1f), woodD);
                for (int s = -1; s <= 1; s += 2) RB(host, "CafeStool", new Vector3(tx + s * 0.85f, fy + 0.42f, tz), new Vector3(0.45f, 0.08f, 0.45f), orange);
                RB(host, "Plate", new Vector3(tx, fy + 0.77f, tz), new Vector3(0.4f, 0.02f, 0.4f), Color.white);
            }
            RB(host, "Planter", new Vector3(ox + rw * 0.5f - 0.5f, fy + 0.35f, oz - rd * 0.5f + 1.2f), new Vector3(0.6f, 0.7f, 0.6f), basalt, true);
            var tree = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(tree.GetComponent<Collider>()); tree.name = "PlanterTree"; tree.transform.SetParent(host, false); tree.transform.position = new Vector3(ox + rw * 0.5f - 0.5f, fy + 1.3f, oz - rd * 0.5f + 1.2f); tree.transform.localScale = Vector3.one * 0.9f; tree.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.35f, 0.62f, 0.32f));
            RoomNpc(host, "Npc_Haenyeo", new Vector3(ox + 1.2f, fy, oz + 2.4f), 180f, Loc.T("사장님: 어서 와요~ 제주 브런치 한 접시 먹고 힘내요!", "Owner: Welcome! Grab a Jeju brunch and recharge!"));
            _spots.Add(new Spot { id = "in_cafe", title = Loc.T("🍊 주문하기 · 제주 브런치 (HP 회복)", "🍊 Order · Jeju brunch (restore HP)"), pos = new Vector3(ox + 1.2f, fy, oz + 0.5f), radius = 2.0f, on = CafeMenu });
            BuildingOutline.Attach(host, 0.018f);
        }

        void CafeMenu()
        {
            if (Save == null) return;
            VillageDex.Talk(Save, VillageDex.NpcIndex("brunch"), 1);   // 195차
            int max = PlayerStats.StatMax;
            var list = new System.Collections.Generic.List<(string, Color, Action)>();
            foreach (var it in Brunch)
            {
                var item = it;
                list.Add((Loc.T($"{item.ko}  ·  HP +{item.hp}  ·  {item.price}G", $"{item.en}  ·  HP +{item.hp}  ·  {item.price}G"), item.col, () => EatBrunch(item)));
            }
            list.Add((Loc.T("다음에 올게요", "Maybe later"), new Color(0.6f, 0.6f, 0.66f), () => { }));
            _hud.Choice(Loc.T("🍊 귤빛 브런치 메뉴", "🍊 Tangerine Brunch menu"), Loc.T($"HP {Save.stats.stamina}/{max}  ·  지갑 {Save.stats.money:N0}G  ·  먹으면 HP 가 차고 스트레스도 조금 풀려요", $"HP {Save.stats.stamina}/{max} · {Save.stats.money:N0}G"), list.ToArray());
        }
        void EatBrunch(BrunchItem item)
        {
            int max = PlayerStats.StatMax;
            if (Save.stats.money < item.price) { _hud.Bubble(Loc.T("사장님", "Owner"), Loc.T($"{item.price}G 가 필요해요. 돈이 모자라네~", $"That's {item.price}G — not enough coins.")); return; }
            if (Save.stats.stamina >= max) { _hud.Bubble(Loc.T("사장님", "Owner"), Loc.T("배가 꽉 찼네! 체력이 가득해요. 다음에 또 와요.", "You're full — HP is maxed. Come again!")); return; }
            int before = Save.stats.stamina;
            Save.ateThisWeek = true;   // 221차: 브런치도 이번 주 식사로 친다(안 치면 주말에 「식사를 안 했다」)
            Save.stats.money -= item.price; Save.stats.stamina = Mathf.Min(max, Save.stats.stamina + item.hp); Save.stats.stress = Mathf.Max(0, Save.stats.stress - item.stress); Save.stats.Clamp();
            _gm.Persist(); RefreshStatus();
            VillagePang.Burst(_player.position + Vector3.up * 1.0f, new Color(1f, 0.70f, 0.30f), Color.white, 1.0f);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.45f);
            CoastToast.Show(Loc.T($"{item.ko} 냠냠! HP {before} → {Save.stats.stamina} · 스트레스 −{item.stress} (−{item.price}G)", $"{item.en}! HP {before} → {Save.stats.stamina} (−{item.price}G)"));
        }

        // ── 버스 정류장 ─────────────────────────────────────────────────
        void BusStopTalk()
        {
            BusMenu(VillageZones.Zone.None); return;   // 195차(사용자: 「정류장에서 버스 타면 시내·관광지」)
            bool here = VillageEast.BusAtStop;
            _hud.Bubble(Loc.T("하늘", "Haneul"), here
                ? Loc.T("버스가 섰다! 201번 · 제주 시내 방면. 오늘은 마을 구경이나 더 하자.", "The bus is here! No. 201 to Jeju City. I'll stay and explore today.")
                : Loc.T("🚌 201번 해안도로 버스 — 10분마다. 시간표 옆에 한라산 사진이 붙어 있다.", "🚌 Coastal bus No. 201 — every 10 min. A Hallasan photo hangs by the timetable."));
        }

        void DevLeaveInterior()
        {
            if (_hud.Locked) _hud.ClosePopup();
            if (_interior != null) { _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_") || s.id == "counter" || s.id.StartsWith("hosp_") || s.id.StartsWith("in_")); Destroy(_interior.gameObject); _interior = null; _interiorKind = null; }
            _busy = false;
        }
        public void DevGoEast(int which)
        {
            DevLeaveInterior();
            if (which == 0) Teleport(VillageWorld.Ground(VillageEast.BankX, VillageEast.BankZ - 6f), 0f);
            else if (which == 1) Teleport(DoorOf(VillageEast.Cafe) + Vector3.forward * 2.5f, 180f);
            else Teleport(VillageWorld.Ground(VillageEast.StopX - 3f, VillageEast.StopZ), 90f);
        }
        public void DevEnterBank() { DevLeaveInterior(); _dayNight?.SetMorning(); _doorCooldown = 0f; EnterBank(); }
        public void DevEnterCafe() { DevLeaveInterior(); _dayNight?.SetMorning(); _doorCooldown = 0f; EnterCafe(); }
        public void DevBankMenu() => BankMenu();
        public void DevCafeMenu() => CafeMenu();
    }
}
