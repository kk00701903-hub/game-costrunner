using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 209차(사용자: 「그래픽, 모션, 몬스터들 움직임 등 디테일 추가 점검 — 인게임에서 움직임 보고 직접 확인」):
    /// 개발용 움직임 계측 — N초 동안 매 프레임 주인공·NPC·몬스터 위치/방향을 기록해 끊김(한 프레임 급회전·순간이동·급출발/급정지·제자리 떨림)과 프레임 시간을 로그로.
    public partial class VillageHub
    {
        public void DevMotionProbe(float secs, string burst = null) { StartCoroutine(MotionProbeCo(secs, burst)); }
        /// 209차: 무엇이 프레임을 먹는지 — 설정을 하나씩 바꿔 가며 3초씩 fps 를 잰다(끝나면 원래대로)
        /// 212차(인게임 플레이테스트): 원격 브릿지는 키를 「누르고 있기」를 못 해서 걷기 테스트용 — Tools/_xfer/walk.txt 한 줄
        /// 「story」 = 이야기 빛으로, 「fwd 6」 = 카메라 앞으로 6 m, 「x z」 = 그 자리로. 조이스틱과 같은 _walkTo 로 걷는다.
        public void DevWalkFile()
        {
            string a = "";
            try { a = System.IO.File.ReadAllText("Tools/_xfer/walk.txt").Trim(); } catch { }
            var p = a.Split(' ');
            Vector3 t;
            if (p[0] == "tp") { var q = p.Length > 2 ? new Vector3(float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)) : _storyPos + new Vector3(0f, 0f, -4f); Teleport(q); Debug.LogWarning($"[212] tp {q}"); return; }
            if (a == "story") t = _storyPos;
            else if (p[0] == "fwd" && p.Length > 1) t = _player.position + Quaternion.Euler(0f, _camYaw, 0f) * Vector3.forward * float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
            else if (p.Length >= 2) t = new Vector3(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
            else { Debug.LogWarning("[212] walk.txt?"); return; }
            WalkTo(t); Debug.LogWarning($"[212] walk {a} -> {t} from {_player.position}");
        }
        public void DevWhere()
        {
            Debug.LogWarning($"[212] pos={_player.position} zone={VillageZones.At(_player.position)} story={_storyPos} busy={_busy} locked={_hud.Locked} cam={(_cam != null ? _cam.transform.position.ToString() : "-")}");
            if (_interior != null) foreach (Transform ch in _interior.transform) if (ch.name.StartsWith("WallUp")) { var r = ch.GetComponent<Renderer>(); Debug.LogWarning($"[213] {ch.name} {ch.position} {ch.localScale} en={(r != null && r.enabled)} active={ch.gameObject.activeInHierarchy}"); }
        }
        public void DevGhost() { if (_creatures != null) _creatures.SpawnGhost(); }
        public void DevNearMob() { if (_dun == null) return; var t = _dun.DevTarget(); if (!t.HasValue) return; var y = _player.position.y; _cc.enabled = false; _player.position = new Vector3(t.Value.x, y, t.Value.z - 3.5f); _player.rotation = Quaternion.identity; _cc.enabled = true; _camYaw = _camYawTarget = 0f; SnapCamera(); }
        public void DevPalmLog()
        {
            var sb = new System.Text.StringBuilder($"[211] palms={VillageHouses.Palms.Count} cam={_cam.transform.position} player={_player.position} hiddenNow={_camHidden.Count}\n");
            var l = new List<Transform>(VillageHouses.Palms); l.RemoveAll(x => x == null); l.Sort((a, b) => Vector3.Distance(a.position, _player.position).CompareTo(Vector3.Distance(b.position, _player.position)));
            for (int i = 0; i < Mathf.Min(4, l.Count); i++) { var r = l[i].GetComponentInChildren<Renderer>(); sb.AppendLine($"[211]  palm {l[i].position} d={Vector3.Distance(l[i].position, _player.position):0.0} rEnabled={(r != null && r.enabled)} kids={l[i].GetComponentsInChildren<Renderer>().Length}"); }
            Debug.LogWarning(sb.ToString());
        }
        public void DevPerfAB() { StartCoroutine(PerfABCo()); }
        IEnumerator PerfABCo()
        {
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            var cd = _cam != null ? _cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
            if (urp == null || cd == null) { Debug.LogWarning("[209] perf: no urp/cam"); yield break; }
            int msaa0 = urp.msaaSampleCount; float rs0 = urp.renderScale; bool sh0 = cd.renderShadows, pp0 = cd.renderPostProcessing; float sd0 = urp.shadowDistance;
            var sb = new System.Text.StringBuilder("[209] perf A/B (3 s each)\n");
            IEnumerator Measure(string name) { yield return new WaitForSecondsRealtime(0.8f); float t0 = Time.realtimeSinceStartup; int f = 0; var d = new List<float>(); while (Time.realtimeSinceStartup - t0 < 3f) { f++; d.Add(Time.unscaledDeltaTime); yield return null; } d.Sort(); sb.AppendLine($"[209]  {name}: fps={f / 3f:0.0} med={d[d.Count / 2] * 1000f:0.0}ms p90={d[(int)(d.Count * 0.9f)] * 1000f:0.0}ms"); }
            for (int rep = 0; rep < 2; rep++)
            {
                urp.msaaSampleCount = msaa0; urp.renderScale = rs0; urp.shadowDistance = sd0; yield return Measure("base");
                urp.msaaSampleCount = 2; yield return Measure("msaa2");
                urp.msaaSampleCount = 2; urp.shadowDistance = 40f; yield return Measure("msaa2+shadow40");
                urp.msaaSampleCount = 2; urp.shadowDistance = sd0; urp.renderScale = 0.9f; yield return Measure("msaa2+scale0.9");
                urp.renderScale = rs0; urp.msaaSampleCount = 1; yield return Measure("msaa1");
            }
            urp.msaaSampleCount = msaa0; urp.renderScale = rs0; cd.renderShadows = sh0; cd.renderPostProcessing = pp0; urp.shadowDistance = sd0;
            int rcount = 0; foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled && r.isVisible) rcount++;
            sb.AppendLine($"[209]  visible renderers={rcount} screen={Screen.width}x{Screen.height}");
            Debug.LogWarning(sb.ToString());
        }
        /// 209차: 프레임이 어디서 먹히는지 — 프로파일러 Recorder 로 스크립트·렌더·GPU 대기 시간(ms/프레임)
        public void DevPerfSplit() { StartCoroutine(PerfSplitCo()); }
        IEnumerator PerfSplitCo()
        {
            string[] names = { "PlayerLoop", "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate", "FixedUpdate.PhysicsFixedUpdate", "RenderPipelineManager.DoRenderLoop_Internal()", "Inl_Draw Opaque", "Inl_MainLightShadow", "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForGfxCommandsFromMainThread", "GC.Collect", "Animators.Update", "UIEvents.CanvasManagerRenderOverlays", "Canvas.SendWillRenderCanvases" };
            var recs = new List<UnityEngine.Profiling.Recorder>(); foreach (var n in names) { var r = UnityEngine.Profiling.Recorder.Get(n); r.enabled = true; recs.Add(r); }
            var sum = new double[names.Length]; int f = 0; float t0 = Time.realtimeSinceStartup; yield return null;
            // 218차: 프레임당 GC 할당(바이트) — 모바일에서 끊김(GC 스파이크)의 주범
            var gc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame"); long gcSum = 0, gcMax = 0; int gcN = 0;
            while (Time.realtimeSinceStartup - t0 < 4f) { for (int i = 0; i < recs.Count; i++) if (recs[i].isValid) sum[i] += recs[i].elapsedNanoseconds / 1e6; if (gc.Valid) { long v = gc.LastValue; gcSum += v; gcN++; if (v > gcMax) gcMax = v; } f++; yield return null; }
            Debug.LogWarning($"[218] GC alloc/frame avg={(gcN > 0 ? gcSum / gcN : 0) / 1024f:0.0} KB max={gcMax / 1024f:0.0} KB (n={gcN})"); gc.Dispose();
            var sb = new System.Text.StringBuilder($"[209] perf split {f} frames (ms/frame)\n");
            for (int i = 0; i < names.Length; i++) sb.AppendLine($"[209]  {names[i]} = {(recs[i].isValid ? (sum[i] / f).ToString("0.00") : "n/a")}");
            sb.AppendLine($"[209]  scripts: Village objects Update — VillageCreatures etc. / GC alloc per frame n/a");
            foreach (var r in recs) r.enabled = false;
            Debug.LogWarning(sb.ToString());
        }
        class Track { public string kind; public Transform t; public Vector3 p; public float yaw, v; public int n, snaps, jumps, jerks, flips; public float maxYaw, maxV, sumV; public float lastDx; }
        IEnumerator MotionProbeCo(float secs, string burst)
        {
            var l = new List<KeyValuePair<string, Transform>>();
            l.Add(new KeyValuePair<string, Transform>("player", _player));
            if (_creatures != null) _creatures.DevTracked(l);
            if (_dun != null) _dun.DevTracked(l);
            if (_player != null) { var pv = _player.Find("Pivot") ?? (_player.childCount > 0 ? _player.GetChild(0) : null); if (pv != null) l.Add(new KeyValuePair<string, Transform>("playerRig", pv)); }
            var tr = new List<Track>();
            foreach (var kv in l) if (kv.Value != null) tr.Add(new Track { kind = kv.Key, t = kv.Value, p = kv.Value.position, yaw = kv.Value.eulerAngles.y });
            float t0 = Time.time, maxDt = 0f; int frames = 0, hitch = 0; var dts = new List<float>();
            Time.captureDeltaTime = 1f / 60f;   // 에디터 fps(장면 뷰까지 그림)에 휘둘리지 않게 게임 시간을 60 fps 고정 걸음으로 — 기기에서 보일 한 프레임씩을 잰다
            yield return null;
            while (Time.time - t0 < secs)
            {
                if (burst != null && frames % 6 == 0 && frames / 6 < 12) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../Tools/_shots/" + burst + "_" + (frames / 6).ToString("00") + ".png"));   // 0.1 초 간격 연속 캡처 12장
                float dt = Time.deltaTime; frames++; dts.Add(dt); if (dt > maxDt) maxDt = dt; if (dt > 1f / 30f) hitch++;
                foreach (var k in tr)
                {
                    if (k.t == null) continue;
                    var p = k.t.position; float yaw = k.t.eulerAngles.y; var d = p - k.p; d.y = 0f;
                    float v = d.magnitude / Mathf.Max(dt, 1e-4f); float dy = Mathf.Abs(Mathf.DeltaAngle(k.yaw, yaw));
                    if (k.n > 0)
                    {
                        if (dy > 25f) k.snaps++;                              // 한 프레임에 25° 넘게 꺾임
                        if (d.magnitude > 1.5f) k.jumps++;                    // 한 프레임에 1.5 m 넘게 이동(순간이동)
                        if (Mathf.Abs(v - k.v) / Mathf.Max(dt, 1e-4f) > 40f && Mathf.Max(v, k.v) > 0.5f) k.jerks++;   // 가속도 40 m/s² 넘는 급출발·급정지
                        float dx = Vector3.Dot(d, k.t.forward); if (k.lastDx * dx < 0f && d.magnitude > 0.004f && d.magnitude < 0.05f) k.flips++;   // 앞뒤로 달달 떨림
                        k.lastDx = dx;
                        if (dy > k.maxYaw) k.maxYaw = dy; if (v > k.maxV) k.maxV = v; k.sumV += v;
                    }
                    k.p = p; k.yaw = yaw; k.v = v; k.n++;
                }
                yield return null;
            }
            Time.captureDeltaTime = 0f;
            dts.Sort(); float med = dts.Count > 0 ? dts[dts.Count / 2] : 0f;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[209] probe {secs:0}s frames={frames} (60fps 고정) medDt={med * 1000f:0.0}ms maxDt={maxDt * 1000f:0}ms hitch(>33ms)={hitch} tracked={tr.Count}");
            var byKind = new Dictionary<string, List<Track>>();
            foreach (var k in tr) { if (!byKind.TryGetValue(k.kind, out var lst)) byKind[k.kind] = lst = new List<Track>(); lst.Add(k); }
            foreach (var kv in byKind)
            {
                int n = kv.Value.Count, snaps = 0, jumps = 0, jerks = 0, flips = 0; float maxYaw = 0f, maxV = 0f, avgV = 0f;
                foreach (var k in kv.Value) { snaps += k.snaps; jumps += k.jumps; jerks += k.jerks; flips += k.flips; maxYaw = Mathf.Max(maxYaw, k.maxYaw); maxV = Mathf.Max(maxV, k.maxV); avgV += k.n > 1 ? k.sumV / (k.n - 1) : 0f; }
                sb.AppendLine($"[209]  {kv.Key} x{n}: snap>25°={snaps} maxYaw/f={maxYaw:0}° jump={jumps} jerk={jerks} jitter={flips} maxV={maxV:0.0} avgV={avgV / n:0.00}");
            }
            Debug.LogWarning(sb.ToString());
        }
    }
}
