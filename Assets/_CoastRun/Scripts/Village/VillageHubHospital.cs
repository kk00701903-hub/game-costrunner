using System;
using System.Collections;
using UnityEngine;

namespace CoastRun.Village
{
    /// 189차(사용자: 「병원도 만들어줘, 병원에서 눈을 뜰 때는 병원 침대 옆에서 보이게」):
    /// 큰길 동쪽 병원(VillageWorld.Hospital) — 들어가면 흰 병실(침대 3·커튼·링거대·빨간 십자), 간호사 책상에서 치료(HP 가득).
    /// 쓰러지면(HP 0·체포) 병원 첫 침대 옆에서 깨어나고, 문으로 나가면 병원 앞.
    public partial class VillageHub
    {
        public const int HospitalCurePrice = 300;   // 치료비(흰밥 한 그릇 ≈ 275G 수준)
        static readonly Color HospWall = new Color(0.95f, 0.97f, 0.98f), HospAccent = new Color(0.62f, 0.84f, 0.90f);

        void EnterHospital()
        {
            if (_busy || _interior != null || VillageWorld.Hospital == null) return;
            _interiorKind = "hospital"; EnterHouse(VillageWorld.Hospital, Loc.T("병원", "Hospital"), HospitalDoor());
        }
        Vector3 HospitalDoor() { foreach (var hs in VillageWorld.Houses) if (hs.house == VillageWorld.Hospital) return hs.door; return VillageWorld.Hospital != null ? VillageWorld.Hospital.position : Vector3.zero; }

        /// 화면이 까만 동안 부른다: 병실을 만들고 첫 침대 옆(카메라 쪽을 보고)에 세운다. 병원이 없으면 false.
        bool WakeInHospital()
        {
            if (VillageWorld.Hospital == null) return false;
            if (_interior != null) { _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_") || s.id == "counter" || s.id.StartsWith("hosp_") || s.id.StartsWith("in_")); Destroy(_interior.gameObject); _interior = null; }
            var house = VillageWorld.Hospital; var door = HospitalDoor();
            _heroInside = false; _interiorKind = "hospital";
            _interior = VillageInterior.Create(transform, Loc.T("병원", "Hospital"), HospWall, HospAccent, door + house.forward * 1.7f, house.eulerAngles.y);
            _spots.Add(new Spot { id = "exit", title = Loc.T("문 · 밖으로 나가기", "Door · Go outside"), pos = _interior.ExitSpot, radius = 1.6f, on = ExitHouse });
            BuildHospitalRoom();
            Teleport(HospitalBedSide(0), 0f);   // 침대 옆에 선다(실내 카메라는 뒤(남쪽)에서 보므로 북쪽을 봐야 침대와 함께 보인다)
            _doorCooldown = Time.time + 3f;
            return true;
        }

        static float BedX(int i) => VillageInterior.OX - 3.0f + i * 2.6f;
        static float BedZ => VillageInterior.OZ + VillageInterior.RD * 0.5f - 1.35f;
        Vector3 HospitalBedSide(int i) => new Vector3(BedX(i) + 1.05f, _interior != null ? _interior.FloorY : 0f, BedZ - 0.4f);

        void BuildHospitalRoom()
        {
            if (_interior == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW;
            var host = new GameObject("HospitalRoom").transform; host.SetParent(_interior.transform, false);
            // 집 가구는 끈다(러그·침대·탁자·책장…)
            foreach (Transform ch in _interior.transform)
            {
                string n = ch.name;
                if (n == "Plank" || n == "Bed" || n == "Mattress" || n == "Pillow" || n == "Blanket" || n == "Headboard" || n == "Skirt" || n == "Table" || n == "Chair" || n == "ChairBack" || n == "Leg" || n == "Shelf" || n == "Book" || n == "Pot" || n == "Plant" || n == "Cup" || n == "Rug" || n == "RugIn" || n == "LampBase" || n == "LampShade" || n == "Curtain") ch.gameObject.SetActive(false);
            }
            GameObject B(string n, Vector3 pos, Vector3 size, Color col, bool collide = false)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); if (!collide) Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(host, false);
                g.transform.position = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(col, 0.1f); return g;
            }
            // 193차(사용자: 「병원 내부 파이어플라이로 다시 그려줘, 너무 하얗다」): Firefly 그림(바닥 민트 체크·복숭아 벽지·하늘 체크 이불·라벤더 커튼·하트 포스터)
            Material TexM(string tex, Vector2 tiling, Color fallback)
            {
                var t = Resources.Load<Texture2D>("CoastRun/Textures/Village/" + tex);
                if (t == null) return CoastMaterials.CreateLit(fallback, 0.1f);
                var m = CoastMaterials.CreateToon(new Color(0.98f, 0.98f, 0.98f), t, 0.05f); m.mainTextureScale = tiling; return m;
            }
            GameObject TB(string n, Vector3 pos, Vector3 size, Material m, bool collide = false) { var g = B(n, pos, size, Color.white, collide); g.GetComponent<MeshRenderer>().sharedMaterial = m; return g; }
            var floorM = TexM("Tex_HospFloor", new Vector2(4f, 3f), new Color(0.80f, 0.93f, 0.86f));
            var wallM = TexM("Tex_HospWall", new Vector2(3f, 1.2f), new Color(1f, 0.88f, 0.80f));
            var wallSideM = TexM("Tex_HospWall", new Vector2(2.4f, 1.2f), new Color(1f, 0.88f, 0.80f));
            var blanketM = TexM("Tex_HospBlanket", new Vector2(1f, 1f), new Color(0.62f, 0.82f, 0.92f));
            var curtainM = TexM("Tex_HospCurtain", new Vector2(1f, 1f), new Color(0.82f, 0.76f, 0.95f));
            foreach (Transform ch in _interior.transform)
            {
                var r = ch.GetComponent<MeshRenderer>(); if (r == null) continue;
                if (ch.name == "Floor") r.sharedMaterial = floorM;
                else if (ch.name == "WallN") r.sharedMaterial = wallM;
                else if (ch.name == "WallW" || ch.name == "WallE") r.sharedMaterial = wallSideM;
                else if (ch.name == "Skirt") r.sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.78f, 0.72f), 0.1f);
            }
            // 벽을 위로 3 m 더 — 낮은 벽 너머로 하늘·구름이 보여 방이 하늘에 떠 보이던 것
            float wh = VillageInterior.WallH;
            TB("HWallUpN", new Vector3(ox, fy + wh + 1.5f, oz + rd * 0.5f), new Vector3(rw, 3f, 0.2f), wallM);
            TB("HWallUpW", new Vector3(ox - rw * 0.5f, fy + wh + 1.5f, oz), new Vector3(0.2f, 3f, rd), wallSideM);
            TB("HWallUpE", new Vector3(ox + rw * 0.5f, fy + wh + 1.5f, oz), new Vector3(0.2f, 3f, rd), wallSideM);
            var metal = new Color(0.55f, 0.62f, 0.70f); var sheet = new Color(0.90f, 0.93f, 0.97f);   // 198차: 너무 하얘서 구분이 안 됨 → 틀·시트 톤 다운 var blanket = new Color(0.62f, 0.82f, 0.92f); var curtain = new Color(0.78f, 0.92f, 0.86f);
            // 바닥 위 연한 민트 타일 띠 · 벽 아래 띠
            B("HospStripe", new Vector3(ox, fy + 0.9f, oz + rd * 0.5f - 0.11f), new Vector3(rw, 0.14f, 0.03f), HospAccent);
            // 침대 3(머리 = 북벽)
            for (int i = 0; i < 3; i++)
            {
                float bx = BedX(i), bz = BedZ;
                B("HBedFrame", new Vector3(bx, fy + 0.30f, bz), new Vector3(1.15f, 0.12f, 2.1f), metal, true);
                foreach (var d in new[] { new Vector3(-0.5f, 0f, -0.95f), new Vector3(0.5f, 0f, -0.95f), new Vector3(-0.5f, 0f, 0.95f), new Vector3(0.5f, 0f, 0.95f) }) B("HBedLeg", new Vector3(bx, fy + 0.15f, bz) + d, new Vector3(0.06f, 0.3f, 0.06f), metal);
                B("HMattress", new Vector3(bx, fy + 0.44f, bz), new Vector3(1.08f, 0.16f, 2.02f), sheet);
                TB("HBlanket", new Vector3(bx, fy + 0.54f, bz - 0.3f), new Vector3(1.12f, 0.06f, 1.35f), blanketM);
                B("HPillow", new Vector3(bx, fy + 0.58f, bz + 0.72f), new Vector3(0.78f, 0.14f, 0.42f), sheet);
                B("HHead", new Vector3(bx, fy + 0.75f, bz + 1.02f), new Vector3(1.15f, 0.9f, 0.06f), metal);
                // 링거대(침대 왼쪽 머리맡) + 수액 봉지
                B("IVPole", new Vector3(bx - 0.78f, fy + 0.9f, bz + 0.7f), new Vector3(0.04f, 1.8f, 0.04f), metal);
                B("IVBag", new Vector3(bx - 0.78f, fy + 1.65f, bz + 0.7f), new Vector3(0.22f, 0.3f, 0.08f), new Color(0.85f, 0.95f, 1f));
                // 침대 사이 커튼(마지막 침대 오른쪽은 없음)
                if (i < 2) TB("HCurtain", new Vector3(bx + 1.3f, fy + 1.15f, bz + 0.1f), new Vector3(0.05f, 1.9f, 1.9f), curtainM);
            }
            // 북벽 가운데 빨간 십자
            B("HCrossPlate", new Vector3(ox + 3.3f, fy + 2.2f, oz + rd * 0.5f - 0.12f), new Vector3(0.9f, 0.9f, 0.04f), Color.white);
            B("HCross", new Vector3(ox + 3.3f, fy + 2.2f, oz + rd * 0.5f - 0.15f), new Vector3(0.66f, 0.2f, 0.03f), new Color(0.92f, 0.20f, 0.24f));
            B("HCross", new Vector3(ox + 3.3f, fy + 2.2f, oz + rd * 0.5f - 0.15f), new Vector3(0.2f, 0.66f, 0.03f), new Color(0.92f, 0.20f, 0.24f));
            // 하트 포스터(Firefly) 두 장 — 침대 사이 벽
            var posterM = TexM("Tex_HospPoster", Vector2.one, new Color(1f, 0.8f, 0.85f));
            for (int k = 0; k < 2; k++)
            {
                float px = BedX(k) + 1.3f;
                B("HPosterFrame", new Vector3(px, fy + 2.35f, oz + rd * 0.5f - 0.13f), new Vector3(1.0f, 1.3f, 0.03f), Color.white);
                var pq = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(pq.GetComponent<Collider>()); pq.name = "HPoster"; pq.transform.SetParent(host, false);
                pq.transform.position = new Vector3(px, fy + 2.35f, oz + rd * 0.5f - 0.16f); pq.transform.rotation = Quaternion.identity; pq.transform.localScale = new Vector3(0.9f, 1.2f, 1f);
                pq.GetComponent<MeshRenderer>().sharedMaterial = posterM;
            }
            // 간호사 책상(동쪽) + 약장
            float dx = ox + rw * 0.5f - 1.2f, dz = oz - 0.4f;
            B("NurseDesk", new Vector3(dx, fy + 0.45f, dz), new Vector3(0.8f, 0.9f, 2.0f), new Color(0.82f, 0.86f, 0.92f), true);
            B("NurseDeskTop", new Vector3(dx, fy + 0.92f, dz), new Vector3(0.9f, 0.05f, 2.1f), HospAccent);
            B("MedCabinet", new Vector3(ox + rw * 0.5f - 0.3f, fy + 1.1f, oz + 1.6f), new Vector3(0.4f, 2.0f, 1.2f), Color.white, true);
            B("MedCross", new Vector3(ox + rw * 0.5f - 0.52f, fy + 1.6f, oz + 1.6f), new Vector3(0.03f, 0.3f, 0.1f), new Color(0.92f, 0.20f, 0.24f));
            B("MedCross", new Vector3(ox + rw * 0.5f - 0.52f, fy + 1.6f, oz + 1.6f), new Vector3(0.03f, 0.1f, 0.3f), new Color(0.92f, 0.20f, 0.24f));
            // 간호사(카페 알바 모델) — 책상 뒤에서 방 쪽(서쪽)을 본다
            var npcRoot = new GameObject("Nurse").transform; npcRoot.SetParent(host, false); npcRoot.position = new Vector3(dx + 0.75f, fy, dz); npcRoot.rotation = Quaternion.Euler(0f, -90f, 0f);
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(npcRoot, false);
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/Npc_Cafe", pivot, 1.0f, true);
            if (rig != null)
            {
                var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, 0.12f); anim.speed = 0f; }
                var mo = pivot.gameObject.AddComponent<CharacterMotion>(); mo.Anim = anim; mo.LookTarget = _player; mo.FootDust = false;
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                StartCoroutine(ClerkFace(anim, rig.gameObject, "Npc_Cafe", npcRoot));
                // 흰 간호사 모자
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(cap.GetComponent<Collider>()); cap.name = "NurseCap"; cap.transform.SetParent(npcRoot, false);
                cap.transform.localPosition = new Vector3(0f, 1.62f, 0f); cap.transform.localScale = new Vector3(0.34f, 0.12f, 0.2f); cap.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(Color.white, 0.1f);
            }
            var bubble = SpeechBubble.Create(npcRoot, 1.75f); bubble.Show(Loc.T("간호사: 몸은 좀 어때요? 아프면 말해요.", "Nurse: How are you feeling?"));
            _spots.Add(new Spot { id = "hosp_nurse", title = Loc.T($"💊 간호사 · 치료받기 (HP 가득, {HospitalCurePrice}G)", $"💊 Nurse · Treatment (full HP, {HospitalCurePrice}G)"), pos = new Vector3(dx - 1.0f, fy, dz), radius = 1.6f, on = HospitalCure });
            BuildingOutline.AttachThick(host, 0.045f);   // 198차: 병원 가구 굵은 윤곽선(침대·커튼·책상)
        }

        void HospitalCure()
        {
            if (Save == null) return;
            VillageDex.Talk(Save, VillageDex.NpcIndex("nurse"), 1);   // 195차
            int max = PlayerStats.StatMax;
            if (Save.stats.stamina >= max) { _hud.Bubble(Loc.T("간호사", "Nurse"), Loc.T("어디도 안 다쳤네요. 건강해요!", "You're perfectly healthy!")); return; }
            _hud.Choice(Loc.T("💊 치료받기", "💊 Treatment"), Loc.T($"HP {Save.stats.stamina}/{max} → {max}  ·  {HospitalCurePrice}G (지금 {Save.stats.money:N0}G)", $"HP {Save.stats.stamina}/{max} → {max}  ·  {HospitalCurePrice}G"),
                new (string, Color, Action)[] {
                    (Loc.T("치료받기", "Get treated"), new Color(0.45f, 0.78f, 0.85f), () =>
                    {
                        if (Save.stats.money < HospitalCurePrice) { _hud.Bubble(Loc.T("간호사", "Nurse"), Loc.T($"치료비 {HospitalCurePrice}G 가 모자라요. 돈을 모아서 와요.", $"You need {HospitalCurePrice}G.")); return; }
                        Save.stats.money -= HospitalCurePrice; Save.stats.stamina = max; _gm.Persist(); RefreshStatus();
                        VillagePang.Burst(_player.position + Vector3.up * 1.0f, new Color(0.55f, 0.95f, 0.75f), Color.white, 1.1f);
                        CoastToast.Show(Loc.T($"💊 치료 완료! HP 가득 (−{HospitalCurePrice}G)", $"💊 Treated! Full HP (−{HospitalCurePrice}G)"));
                    }),
                    (Loc.T("괜찮아요", "I'm fine"), new Color(0.6f, 0.6f, 0.66f), () => { }),
                });
        }

        public void DevExitHouse() { if (_hud.Locked) _hud.ClosePopup(); ExitHouse(); }
        public void DevAutoMenu() { if (_hud.Locked) _hud.ClosePopup(); AutoMoveMenu(); }
        public void DevGo(float x, float z, float yaw) { if (_hud.Locked) _hud.ClosePopup(); Teleport(VillageWorld.Ground(x, z), yaw); }
        public void DevHospital() { StartCoroutine(Hospital()); }
        public void DevEnterHospital() { EnterHospital(); }
    }
}
