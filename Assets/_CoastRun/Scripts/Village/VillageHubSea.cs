using UnityEngine;

namespace CoastRun.Village
{
    /// 192차(사용자: 「바닷가는 들어는 갈 수 있게 해 주고 10초 이상 들어가면 병원행」):
    /// 발이 바닷물 아래(지형 높이 < 해수면 −0.15)면 시간을 잰다 — 1초에 안내, 7초에 경고, 10초면 파도에 휩쓸려 병원(쓰러짐과 같은 처리).
    /// 물 밖으로 나오면 시간은 두 배 빠르게 줄어든다.
    public partial class VillageHub
    {
        public const float SeaLimit = 10f;
        float _seaT; int _seaWarn; bool _hospSea;
        void TickSea()
        {
            if (_player == null || Save == null) return;
            bool inWater = _interior == null && !_busy && !_hud.Locked && VillageWorld.Height(_player.position.x, _player.position.z) < VillageWorld.SeaLevel - 0.15f;
            if (!inWater) { _seaT = Mathf.Max(0f, _seaT - Time.deltaTime * 2f); if (_seaT <= 0f) _seaWarn = 0; return; }
            _seaT += Time.deltaTime;
            if (_seaWarn == 0 && _seaT > 1f) { _seaWarn = 1; CoastToast.Show(Loc.T($"🌊 바다에 들어왔다! {SeaLimit:0}초 넘게 있으면 위험해", $"🌊 In the sea! Over {SeaLimit:0}s is dangerous")); }
            if (_seaWarn == 1 && _seaT > SeaLimit - 3f) { _seaWarn = 2; CoastToast.Show(Loc.T("🌊 파도가 세다! 3초 안에 나가야 해!", "🌊 Big waves! Get out in 3 seconds!")); CoastPrefs.VibrateEvent(); }
            if (_seaT >= SeaLimit)
            {
                _seaT = 0f; _seaWarn = 0; _walkTo = null;
                if (_autoCo != null) { StopCoroutine(_autoCo); _autoCo = null; _hud.SetButtonOn("AutoMove", false); }
                if (_autoHunt) SetAutoHunt(false);
                CoastToast.Show(Loc.T("🌊 파도에 휩쓸렸다…", "🌊 Swept away by the waves…"));
                _hospSea = true; StartCoroutine(Hospital());
            }
        }
        /// 192차 점검: 여러 자리로 순간이동하며 화면을 Tools/_shots/tour_N.png 로 찍는다
        public void DevTour() { StartCoroutine(DevTourCo()); }
        System.Collections.IEnumerator DevTourCo()
        {
            var pts = new (float x, float z, float yaw)[] { (0f, 26f, 180f), (2f, -2f, 180f), (-4f, -24f, 200f), (-11f, -40f, 180f), (18f, 12f, 90f), (24f, -3f, 120f), (-18f, 25f, 270f), (-8f, 30f, 300f), (16f, -16f, 150f), (30f, -12f, 90f), (-30f, -8f, 250f), (10f, 36f, 60f) };
            for (int i = 0; i < pts.Length; i++)
            {
                DevGo(pts[i].x, pts[i].z, pts[i].yaw); _doorCooldown = Time.time + 999f;
                yield return new WaitForSeconds(1.2f);
                if (_hud.Locked) _hud.ClosePopup();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, $"../Tools/_shots/tour_{i}.png"));
                yield return new WaitForSeconds(0.6f);
            }
            _doorCooldown = Time.time + 2f; Debug.LogWarning("[Tour] done");
        }
        public void DevFadeLog()
        {
            var sb = new System.Text.StringBuilder($"[Fade] cam={_cam.transform.position} n={_fades.Count}");
            foreach (var f in _fades) { if (f.root == null) continue; var bb = f.b; bb.Expand(3f); sb.Append($" | {f.root.name}@{f.root.position.x:F0},{f.root.position.z:F0} a={f.a:F2} sw={f.swapped} in={bb.Contains(_cam.transform.position)} b={f.b.center}/{f.b.size}"); }
            Debug.LogWarning(sb.ToString());
        }
        public void DevFaceBird()
        {
            var b = FindAnyObjectByType<VillageWorld.BirdFly>(); if (b == null) return;
            var p = b.transform.position; var from = new Vector3(p.x, 0f, p.z + 14f);
            if (VillageWorld.Height(from.x, from.z) < VillageWorld.SeaLevel) from = new Vector3(-2.5f, 0f, -23f);
            DevGo(from.x, from.z, Quaternion.LookRotation(new Vector3(p.x - from.x, 0f, p.z - from.z)).eulerAngles.y);
            Debug.LogWarning("[Bird] " + p + " from " + from);
        }
        public void DevFaceFlock()
        {
            var f = VillageSky.I != null ? VillageSky.I.transform.Find("Flock") : null; if (f == null) return;
            var d = f.position - _player.position; d.y = 0f;
            DevGo(0f, 20f, Quaternion.LookRotation(new Vector3(f.position.x - 0f, 0f, f.position.z - 20f)).eulerAngles.y);
            Debug.LogWarning("[Flock] " + f.position);
        }
        public void DevGoSea() { DevGo(0f, -32f, 180f); }
    }
}
