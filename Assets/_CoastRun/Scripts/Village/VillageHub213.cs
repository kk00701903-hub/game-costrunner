using UnityEngine;

namespace CoastRun.Village
{
    /// 213차(사용자: 「단점들 커버해줘」 — 212차 플레이테스트 평가의 단점 보강)
    public partial class VillageHub
    {
        /// 이야기 빛(꼬마가 데려가는 자리)이 이 문 5 m 안에 켜져 있으면 문으로 자동 입장하지 않는다 —
        /// 212차: 가게 앞 빛으로 걸어가면 대사보다 먼저 가게 안으로 들어가 버려 「여기가 가게야」가 나온 뒤로 밀렸다.
        bool StoryHoldsDoor(Vector3 door)
        {
            if (_storyBeacon == null || !_storyBeacon.gameObject.activeSelf || _storyPlaying) return false;
            var d = _storyPos - door; d.y = 0f; return d.magnitude < 5f;
        }
        /// 병원 치료비(%) — 212차: 바다 10초 한 번에 HP·돈 20 %·하루가 한꺼번에 날아가 초보가 크게 좌절.
        /// 첫 2주(week 1·2)는 무료, 바다에 휩쓸린 경우는 10 %, 나머지(밤·갱도)는 예전처럼 20 %.
        int HospitalFeePct()
        {
            if (Save != null && Save.week <= 2) return 0;
            return _hospSea ? 10 : 20;
        }
        /// 212차 평가: 마을상점(처음 들어가는 가게)은 211차 소품 교체(BuildZoneRoom 가게만) 대상이 아니라 상자 계산대·상자 선반 그대로였다.
        /// 상자는 충돌만 남기고 숨기고, 211차 블렌더 모델(VShopCounter · VShelfUnit · VGoodsBox)과 나무 화분으로 채운다. 알바나라는 계산대만.
        void DressCounter213(bool shop)
        {
            if (_interior == null) return;
            var host = _interior.transform.Find(shop ? "ShopCounter" : "JobCounter"); if (host == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rw = VillageInterior.RW, rd = VillageInterior.RD;
            GameObject Put(string model, Vector3 at, float yaw) { var g = JejuKit.Spawn(model, host, Vector3.zero, yaw, 1f); if (g != null) g.transform.position = at; return g; }
            bool counterOk = Put("VShopCounter", new Vector3(ox, fy, oz + 1.5f), 180f) != null;
            foreach (Transform t in host)
            {
                var r = t.GetComponent<MeshRenderer>(); if (r == null) continue;
                string n = t.name;
                if (counterOk && (n == "Counter" || n == "CounterTop" || n == "Register" || n == "Basket")) r.enabled = false;
                if (shop && (n == "Shelf" || n == "Goods")) r.enabled = false;
            }
            if (!shop) return;
            // 옆벽 선반 2개씩(방 안쪽을 본다) · 뒷벽 선반 2개(점원 양옆) · 계산대 앞 상품 상자 · 앞 모서리 화분
            float sx = rw * 0.5f - 0.5f;
            foreach (float z in new[] { -1.4f, 1.1f }) { Put("VShelfUnit", new Vector3(ox - sx, fy, oz + z), 90f); Put("VShelfUnit", new Vector3(ox + sx, fy, oz + z), -90f); }
            foreach (float x in new[] { -3.0f, 3.0f }) Put("VShelfUnit", new Vector3(ox + x, fy, oz + rd * 0.5f - 0.5f), 180f);
            foreach (float x in new[] { -2.4f, 2.4f }) Put("VGoodsBox", new Vector3(ox + x, fy, oz + 0.2f), 180f);
            foreach (float x in new[] { -rw * 0.5f + 0.7f, rw * 0.5f - 0.7f }) Put("Kerb_PlanterWood", new Vector3(ox + x, fy, oz - rd * 0.5f + 0.7f), 0f);
            BuildingOutline.Attach(host, 0.018f);
        }

        /// 213차: 병원 대기 의자·화분(212차: 병원도 침대 말고는 비어 보였다)
        void DressHospital213()
        {
            if (_interior == null) return;
            var host = _interior.transform.Find("HospitalRoom"); if (host == null) return;
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rw = VillageInterior.RW, rd = VillageInterior.RD;
            GameObject Put(string model, Vector3 at, float yaw) { var g = JejuKit.Spawn(model, host, Vector3.zero, yaw, 1f); if (g != null) g.transform.position = at; return g; }
            Put("Prop_Bench", new Vector3(ox - rw * 0.5f + 0.6f, fy, oz - 1.6f), 90f);
            Put("Kerb_PlanterWood", new Vector3(ox - rw * 0.5f + 0.7f, fy, oz - rd * 0.5f + 0.7f), 0f);
            Put("Kerb_PlanterWood", new Vector3(ox + rw * 0.5f - 0.7f, fy, oz - rd * 0.5f + 0.7f), 0f);
        }
        /// 213차(개발): 낚시 화면 배경을 마을 3D 에서 한 장 찍어 굽는다(Firefly 생성 한도가 다 차서 대신). Tools/_xfer/fishcam.txt 「x y z  lx ly lz  fov」,
        /// 결과는 Tools/_shots/fishbg_test.png (save=true 면 Resources/CoastRun/Textures/Village/Tex_FishingBG.png 에도)
        public void DevCaptureFishingBG(bool save)
        {
            float[] v = { 4f, 2.6f, -30f, 30f, 0f, -60f, 55f };
            try { var p = System.IO.File.ReadAllText("Tools/_xfer/fishcam.txt").Trim().Split(' '); for (int i = 0; i < Mathf.Min(7, p.Length); i++) v[i] = float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture); } catch { }
            var cam = _cam; if (cam == null) return;
            var pos0 = cam.transform.position; var rot0 = cam.transform.rotation; float fov0 = cam.fieldOfView; var tt0 = cam.targetTexture;
            var rt = new RenderTexture(810, 1440, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            var hudOn = _hud != null && _hud.Root != null && _hud.Root.gameObject.activeSelf; if (hudOn) _hud.Root.gameObject.SetActive(false);
            var bubbles = new System.Collections.Generic.List<GameObject>();
            foreach (var sb in FindObjectsByType<SpeechBubble>(FindObjectsSortMode.None)) if (sb.gameObject.activeSelf) { sb.gameObject.SetActive(false); bubbles.Add(sb.gameObject); }
            cam.transform.position = new Vector3(v[0], v[1], v[2]); cam.transform.LookAt(new Vector3(v[3], v[4], v[5])); cam.fieldOfView = v[6];
            cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(810, 1440, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 810, 1440), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = tt0; cam.transform.position = pos0; cam.transform.rotation = rot0; cam.fieldOfView = fov0; rt.Release();
            foreach (var b in bubbles) if (b != null) b.SetActive(true);
            if (hudOn) _hud.Root.gameObject.SetActive(true);
            var png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes("Tools/_shots/fishbg_test.png", png);
            if (save) System.IO.File.WriteAllBytes("Assets/Resources/CoastRun/Textures/Village/Tex_FishingBG.png", png);
            Debug.LogWarning($"[213] fishing bg captured save={save} cam=({v[0]},{v[1]},{v[2]})→({v[3]},{v[4]},{v[5]}) fov {v[6]}");
        }
    }
}
