#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using CoastRun.Village;

namespace CoastRun.DevQA
{
    /// 220차(사용자: 「자동이동을 위해 도로 정비 — 도로 따라 자동이동 장소, 도로엔 장애물 없음」): 도로·자동이동 점검(에디터 전용).
    ///  mode 0: 정적 — 도로망 칸마다 몸 크기 캡슐로 막힌 물체 찾기 + 목적지마다 도로 연결/경로 위 장애물.
    ///  mode 1: 실제 걷기 — 우리집 앞에서 목적지마다 자동이동을 걸어 도착 시간·막힘 기록.
    public class RoadQA220 : MonoBehaviour
    {
        public static void Begin(int mode) { if (!Application.isPlaying) { Debug.LogError("[RoadQA] 플레이 중에만"); return; } var go = new GameObject("RoadQA220"); DontDestroyOnLoad(go); go.AddComponent<RoadQA220>()._mode = mode; }
        int _mode;
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        readonly StringBuilder _r = new StringBuilder(); string _file;
        void Log(string s) { _r.AppendLine(s); Debug.LogWarning("[RoadQA] " + s); try { File.AppendAllText(_file, s + "\n", new UTF8Encoding(false)); } catch { } }
        static object F(object o, string n) { var f = o.GetType().GetField(n, BF); return f != null ? f.GetValue(o) : null; }
        static object M(object o, string n, params object[] a) { var m = o.GetType().GetMethod(n, BF); return m != null ? m.Invoke(o, a) : null; }

        static bool Ignore(Collider c)
        {
            if (c == null || c.isTrigger) return true;
            string n = c.name;
            if (n == "Terrain" || n == "Sea" || n.StartsWith("Ground") || n.Contains("Road") || n.Contains("Path") || n == "Bound" || n.Contains("Floor") || n.Contains("Water")) return true;
            if (c is TerrainCollider) return true;
            var hub = VillageHub.I; if (hub != null) { var pl = F(hub, "_player") as Transform; if (pl != null && c.transform.IsChildOf(pl)) return true; }
            if (c.GetComponentInParent<VillageCreatures>() != null) return true;
            return false;
        }
        static string PathOf(Transform t) { var s = t.name; for (int i = 0; i < 3 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; } return s; }
        readonly Collider[] _buf = new Collider[32];
        List<Collider> Blockers(Vector3 p)
        {
            var list = new List<Collider>();
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.45f, p + Vector3.up * 1.45f, 0.38f, _buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!Ignore(_buf[i])) list.Add(_buf[i]);
            return list;
        }

        IEnumerator Start()
        {
            if (_mode == 1 || _mode == 3 || _mode == 4)
            {   // 세이브·코인 백업 → 플레이 종료 후 자동 복원(MovementQA219Menu)
                string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
                foreach (var f in new[] { "save_0.json", "profile.json" }) { string p = Path.Combine(Application.persistentDataPath, f); if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), ""); }
                EditorPrefs.SetString(MovementQA219.RestoreKey, bk); EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));
            }
            _file = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa", _mode == 0 ? "road_qa_220.txt" : _mode == 4 ? "visibility_qa_220.txt" : "road_walk_qa_220.txt"); Directory.CreateDirectory(Path.GetDirectoryName(_file)); File.WriteAllText(_file, "", new UTF8Encoding(false));
            float w = 0f; while ((VillageHub.I == null || GameManager.I == null || GameManager.I.Save == null) && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSeconds(2f);
            Log($"=== Road QA 220 · {System.DateTime.Now:yyyy-MM-dd HH:mm} · mode={_mode} ===");
            if (_mode == 0) Static(); else if (_mode == 2) MapPng(); else if (_mode == 3) yield return Shots(); else if (_mode == 4) yield return Visibility(); else if (_mode == 5) yield return FadeDebug(); else yield return Walk();
            Log("=== 끝 ===");
        }

        void Static()
        {
            var hub = VillageHub.I; var save = GameManager.I.Save;
            // 1) 길 가운데선(돌길 곡선) 0.5 m 간격, 가운데와 양옆 0.9 m — 걷는 줄
            Log($"길 정비: 옮김 {VillageRoadNet.Moved} · 치움 {VillageRoadNet.Removed}");
            foreach (var l in VillageRoadNet.Report) Log("  " + l);
            var found = new Dictionary<Collider, string>();
            int ci = 0;
            foreach (var C in VillageRoadNet.Curves())
            {
                ci++; float acc = 0f;
                for (int i = 0; i < C.Length - 1; i++)
                {
                    var dir = (C[i + 1] - C[i]).normalized; var side = new Vector2(-dir.y, dir.x);
                    foreach (float o in new[] { 0f, -0.9f, 0.9f })
                    {
                        var pp = C[i] + side * o; if (VillageWorld.InGap(pp.x, pp.y)) continue; var p = VillageWorld.Ground(pp.x, pp.y);
                        foreach (var c in Blockers(p)) if (!found.ContainsKey(c)) found[c] = $"길{ci} ({pp.x:0.0},{pp.y:0.0}) 가운데서 {o:+0.0;-0.0;0} m";
                    }
                }
            }
            Log($"\n## 도로 위 막힌 물체 {found.Count}개");
            foreach (var kv in found) { var b = kv.Key.bounds; Log($"- {PathOf(kv.Key.transform)} [{kv.Key.GetType().Name}] 크기 {b.size.x:0.0}×{b.size.z:0.0}×{b.size.y:0.0} 중심 ({b.center.x:0.0},{b.center.z:0.0}) ← {kv.Value}"); }

            // 2) 목적지 — 자동이동 메뉴 장소 + 이야기 장소
            var spots = F(hub, "_spots") as IList;
            var dest = new List<(string id, Vector3 pos)>();
            foreach (var id in new[] { "hero", "mom", "hospital", "cafe", "shop", "job", "bus", "garden", "farm", "ranch", "barn", "orchard", "hive", "mine", "beach", "play", "light", "tower", "house_해녀네" })
                foreach (var sp in spots) if ((string)F(sp, "id") == id) { dest.Add((id, (Vector3)F(sp, "pos"))); break; }
            foreach (var sc in VillageStory.Order) dest.Add(("이야기 " + sc.id, (Vector3)M(hub, "ScenePos", sc)));
            foreach (var fr in VillageStory.Frags) dest.Add(("이야기 " + fr.id, (Vector3)M(hub, "ScenePos", fr)));
            var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 5.6f)) : new Vector3(0f, 0f, 30f);
            Log($"\n## 목적지 {dest.Count}곳 — 우리집 앞 ({home.x:0},{home.z:0}) 에서");
            foreach (var (id, pos) in dest)
            {
                var r = VillageRoad.Route(save, home, pos, out var why);
                float rd = VillageWorld.PathDist(pos.x, pos.z);
                string gapInfo = "";
                // 경로 위(마지막 도로칸 → 목적지 포함) 장애물
                var hits = new HashSet<string>();
                if (r != null)
                {
                    Vector3 prev = home;
                    foreach (var pt in r)
                    {
                        int n = Mathf.CeilToInt(Vector3.Distance(prev, pt) / 0.5f);
                        for (int s = 1; s <= n; s++) { var p = Vector3.Lerp(prev, pt, s / (float)n); p = VillageWorld.Ground(p.x, p.z); if (Vector3.Distance(p, pos) < 1.2f) continue; foreach (var c in Blockers(p)) hits.Add(PathOf(c.transform) + $"@({p.x:0},{p.z:0})"); }
                        prev = pt;
                    }
                }
                Log($"- {id} ({pos.x:0.0},{pos.z:0.0}) 길까지 {rd:0.0} m · 경로 {(r != null ? "있음 " + r.Count + "점" : "없음(" + why + ")")}{gapInfo}{(hits.Count > 0 ? " · 경로 위 막힘: " + string.Join(", ", hits) : "")}");
            }
        }


        /// mode 2: 마을 지도 PNG(0.5 m 한 칸) — 빨강 막힘 · 파랑 바다 · 노랑 길 가운데선 · 주황 끊긴 곳 · 초록 점 목적지
        void MapPng()
        {
            var hub = VillageHub.I;
            float x0 = -45f, x1 = 80f, z0 = -72f, z1 = 62f, c = 0.5f; int W = Mathf.CeilToInt((x1 - x0) / c), H = Mathf.CeilToInt((z1 - z0) / c);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var buf = new Collider[16];
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                float x = x0 + (i + 0.5f) * c, z = z0 + (j + 0.5f) * c; float h = VillageWorld.Height(x, z);
                Color col = h < VillageWorld.SeaLevel ? new Color(0.25f, 0.45f, 0.75f) : Color.Lerp(new Color(0.55f, 0.72f, 0.45f), new Color(0.95f, 0.95f, 0.85f), Mathf.InverseLerp(0f, 14f, h));
                int n = Physics.OverlapBoxNonAlloc(new Vector3(x, h + 0.95f, z), new Vector3(c * 0.5f, 0.6f, c * 0.5f), buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++) if (!Ignore(buf[k])) { var b = buf[k].bounds.size; col = (b.x > 4f || b.z > 4f) ? new Color(0.55f, 0.1f, 0.1f) : new Color(0.95f, 0.2f, 0.2f); break; }
                tex.SetPixel(i, j, col);
            }
            void Plot(Vector2 p, Color col, int r = 0) { int i = Mathf.RoundToInt((p.x - x0) / c), j = Mathf.RoundToInt((p.y - z0) / c); for (int a = -r; a <= r; a++) for (int b = -r; b <= r; b++) if (i + a >= 0 && i + a < W && j + b >= 0 && j + b < H) tex.SetPixel(i + a, j + b, col); }
            var lines = new List<Vector2[]> { VillageWorld.Path, VillageWorld.RanchPath }; lines.AddRange(VillageWorld.Branches);
            foreach (var L in lines) for (int k = 0; k < L.Length - 1; k++) { int n = Mathf.CeilToInt(Vector2.Distance(L[k], L[k + 1]) / 0.25f); for (int s = 0; s <= n; s++) Plot(Vector2.Lerp(L[k], L[k + 1], s / (float)n), new Color(1f, 0.9f, 0.1f)); }
            foreach (var g in VillageWorld.RoadGaps) for (int a = 0; a < 40; a++) Plot(g + new Vector2(Mathf.Cos(a * 0.157f), Mathf.Sin(a * 0.157f)) * VillageWorld.GapR, new Color(1f, 0.5f, 0f));
            var spots = F(hub, "_spots") as IList; var sb = new StringBuilder();
            foreach (var sp in spots) { var id = (string)F(sp, "id"); var p = (Vector3)F(sp, "pos"); if (p.x < x0 || p.x > x1 || p.z < z0 || p.z > z1) continue; Plot(new Vector2(p.x, p.z), new Color(0.1f, 0.9f, 0.3f), 2); sb.AppendLine($"{id}\t{p.x:0.0}\t{p.z:0.0}"); }
            foreach (var hs in VillageWorld.Houses) if (hs.house != null) { sb.AppendLine($"door:{hs.name}\t{hs.door.x:0.0}\t{hs.door.z:0.0}"); Plot(new Vector2(hs.door.x, hs.door.z), new Color(0.2f, 0.2f, 1f), 1); }
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa");
            File.WriteAllBytes(Path.Combine(dir, "village_map_220.png"), tex.EncodeToPNG());
            File.WriteAllText(Path.Combine(dir, "village_map_220_spots.txt"), $"x0={x0} z0={z0} cell={c} W={W} H={H}\n" + sb, new UTF8Encoding(false));
            Log($"지도 {W}×{H} 저장");
        }


        /// mode 3: 도로 위 막힘 자리마다 순간이동해 스크린샷(Builds/qa/road/)
        public static Vector3[] ShotAt = {
            new Vector3(-5.8f, 0, 26.6f), new Vector3(-4.2f, 0, 24.3f), new Vector3(-4.0f, 0, -3.5f), new Vector3(-6.8f, 0, -9.5f), new Vector3(-2.9f, 0, -19.5f),
            new Vector3(-2.6f, 0, -23.2f), new Vector3(-3.4f, 0, -26.3f), new Vector3(-7.5f, 0, -30.2f), new Vector3(-14.4f, 0, 23.9f), new Vector3(-11.2f, 0, 23.2f),
            new Vector3(-22.4f, 0, 28.4f), new Vector3(25.2f, 0, -6.6f), new Vector3(46.7f, 0, 2.6f), new Vector3(66.6f, 0, 7.4f), new Vector3(-12.3f, 0, -49.2f) };
        IEnumerator Shots()
        {
            var hub = VillageHub.I; string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/road"); Directory.CreateDirectory(dir);
            var cam = F(hub, "_cam") as Camera;
            for (int i = 0; i < ShotAt.Length; i++)
            {
                var p = ShotAt[i]; hub.Teleport(p, 0f);
                for (int k = 0; k < 20; k++) yield return null;
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"road_{i:00}_{p.x:0}_{p.z:0}.jpg"), tex.EncodeToJPG(60)); Destroy(tex);
                Log($"shot {i} ({p.x},{p.z})");
            }
        }


        /// mode 4: 가림 점검 — 길을 따라 8 m 마다 + 장소마다 서서, 카메라→주인공 사이에 불투명한 물체가 남아 있는지(가림 처리 뒤)
        IEnumerator Visibility()
        {
            var hub = VillageHub.I; var cam = F(hub, "_cam") as Camera; var pl = F(hub, "_player") as Transform;
            var pts = new List<(string, Vector3)>();
            int ci = 0; foreach (var C in VillageRoadNet.Curves()) { ci++; float acc = 8f; for (int i = 0; i < C.Length; i++) { if (i > 0) acc += Vector2.Distance(C[i], C[i - 1]); if (acc >= 8f) { acc = 0f; pts.Add(($"길{ci}", VillageWorld.Ground(C[i].x, C[i].y))); } } }
            var spots = F(hub, "_spots") as IList; foreach (var sp in spots) { var id = (string)F(sp, "id"); var p = (Vector3)F(sp, "pos"); if (p.x < -60 || p.x > 90 || p.z < -80 || p.z > 70) continue; pts.Add(("장소 " + id, p)); }
            foreach (var sc in VillageStory.Order) pts.Add(("이야기 " + sc.id, (Vector3)M(hub, "ScenePos", sc)));
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/vis"); Directory.CreateDirectory(dir);
            int bad = 0, shots = 0; var tally = new Dictionary<string, int>();
            foreach (var (name, p) in pts)
            {
                if (F(hub, "_interior") as Object != null) { M(hub, "ExitHouse"); yield return new WaitForSeconds(1.2f); }
                hub.Teleport(p, float.NaN);
                for (int k = 0; k < 14; k++) yield return null;
                var head = pl.position + Vector3.up * 1.0f; var cp = cam.transform.position; var dv = head - cp; float len = dv.magnitude; var ray = new Ray(cp, dv / len);
                var occ = new List<string>();
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                    if (r.transform.IsChildOf(pl) || r.GetComponentInParent<VillageCreatures>() != null || r.GetComponentInParent<Canvas>() != null) continue;
                    var b = r.bounds; if (b.size.x > 60f || b.size.z > 60f) continue;   // 지형·바다
                    string rn = r.gameObject.name; if (r.transform.parent != null && r.transform.parent.name.StartsWith("PathMesh")) continue;   // 224차: 길 테두리 선(바닥에 깔림)은 가림 아님
                    if (rn.Contains("Shadow") || rn.Contains("Blob") || rn.Contains("Terrain") || rn.Contains("Sea") || rn.Contains("Path") || rn.Contains("Road") || rn.Contains("Sky") || rn.Contains("Weather") || rn.Contains("Beacon") || rn.Contains("Glow")) continue;
                    var m = r.sharedMaterial; if (m != null && m.renderQueue >= 2900) continue;   // 반투명(유령·빛)
                    b.Expand(-0.15f);
                    if (b.IntersectRay(ray, out float hd) && hd < len - 0.6f && hd > 0.5f)
                    {
                        // 경계 상자는 넉넉하다 — 실제로 가리는지 가운데 줄에서 한 번 더(메시 콜라이더 없으니 경계 상자 가운데 70% 안인지)
                        var inner = r.bounds; inner.Expand(-Mathf.Min(inner.size.x, inner.size.z) * 0.3f);
                        if (!inner.IntersectRay(ray, out float h2) || h2 > len - 0.6f) continue;
                        occ.Add(PathOf(r.transform));
                    }
                }
                if (occ.Count > 0)
                {
                    bad++; foreach (var o in occ) { string key = o.Split('/')[o.Split('/').Length - 1]; tally[key] = tally.TryGetValue(key, out var v) ? v + 1 : 1; }
                    Log($"- {name} ({p.x:0},{p.z:0}) 가림: {string.Join(", ", occ)}");
                    if (shots < 40) { yield return new WaitForEndOfFrame(); var tex = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(dir, $"vis_{shots:00}_{p.x:0}_{p.z:0}.jpg"), tex.EncodeToJPG(55)); Destroy(tex); shots++; }
                }
            }
            Log($"점검 {pts.Count}곳 · 가림 남음 {bad}곳");
            foreach (var kv in tally) Log($"  {kv.Key} ×{kv.Value}");
        }


        IEnumerator FadeDebug()
        {
            var hub = VillageHub.I; var cam = F(hub, "_cam") as Camera; var pl = F(hub, "_player") as Transform;
            foreach (var p in new[] { new Vector3(9f, 0f, 31f), new Vector3(50f, 0f, 15.3f), new Vector3(-24f, 0f, 53.9f) })
            {
                hub.Teleport(p, float.NaN); for (int k = 0; k < 30; k++) yield return null;
                var fades = F(hub, "_fades") as IList; var occ = F(hub, "_occHouses") as ICollection;
                var head = pl.position + Vector3.up; var cp = cam.transform.position; var d = cp - head; var ray = new Ray(head, d.normalized);
                Log($"@({p.x},{p.z}) cam ({cp.x:0.0},{cp.y:0.0},{cp.z:0.0}) fades={fades?.Count} occ={occ?.Count}");
                if (fades != null) foreach (var f in fades)
                {
                    var root = F(f, "root") as Transform; if (root == null) continue; var b = (Bounds)F(f, "b");
                    if ((root.position - p).magnitude > 15f) continue;
                    bool hit = b.IntersectRay(ray, out float hd);
                    Log($"   fade {PathOf(root)} b={b.center:F1}/{b.size:F1} a={F(f, "a")} swapped={F(f, "swapped")} hit={hit} hd={hd:0.0} len={d.magnitude:0.0} containsHead={b.Contains(head)}");
                }
                foreach (var hs in VillageWorld.Houses) if (hs.house != null && (hs.house.position - p).magnitude < 15f) Log($"   house {PathOf(hs.house)} {hs.name}");
            }
        }

        IEnumerator Walk()
        {
            var hub = VillageHub.I; var save = GameManager.I.Save;
            VillageHub.DevAutoBubble = true;
            var spots = F(hub, "_spots") as IList;
            var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 5.6f)) : new Vector3(0f, 0f, 30f);
            var ids = new[] { "job", "mom", "hospital", "cafe", "shop", "bus", "garden", "farm", "ranch", "barn", "orchard", "hive", "mine", "beach", "play", "light", "tower", "lot_0", "lot_1", "lot_2", "lot_3" };
            // 끊긴 길로 잠긴 곳도 걸어 보려고, 시험 동안만 끊긴 자리에 도로 칸을 깐 것으로(세이브는 끝나고 복원)
            if (save.roadCells == null) save.roadCells = new List<int>();
            foreach (var g in VillageWorld.RoadGaps)
                for (float dx = -4f; dx <= 4f; dx += 1f) for (float dz = -4f; dz <= 4f; dz += 1f)
                { int k = VillageRoad.KeyAt(new Vector3(g.x + dx, 0f, g.y + dz)); if (!save.roadCells.Contains(k)) save.roadCells.Add(k); }
            int ok = 0, fail = 0;
            foreach (var id in ids)
            {
                object sp = null; foreach (var s in spots) if ((string)F(s, "id") == id) { sp = s; break; }
                if (sp == null) { Log($"- {id}: 스팟 없음"); continue; }
                var pos = (Vector3)F(sp, "pos"); float rad = (float)F(sp, "radius");
                if (F(hub, "_interior") as Object != null) { M(hub, "ExitHouse"); yield return new WaitForSeconds(1.5f); }
                hub.Teleport(home, 180f); yield return new WaitForSeconds(0.6f);
                // 224차: 세이브 진행에 따라 이야기 컷씬이 저절로 시작될 수 있다 — 끝날 때까지 기다린 뒤 걷기(전엔 90초 묶여 「못 감」으로 셌다)
                { float sw = 0f; bool waited = false; while (((bool)F(hub, "_storyPlaying") || CinematicPlayer.IsPlaying) && sw < 400f) { waited = true; sw += Time.deltaTime; if (!CinematicPlayer.IsPlaying && Time.frameCount % 45 == 0) { var hud0 = F(hub, "_hud") as VillageHud; if (hud0 != null && hud0.PopupOpen) hud0.ClosePopup(); foreach (var cv0 in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) { if (cv0 == null || !cv0.isRootCanvas || !cv0.isActiveAndEnabled || cv0.sortingOrder < 150 || cv0.name.Contains("Hud") || cv0.name.Contains("Toast") || cv0.name.Contains("Cinematic")) continue; foreach (var b0 in cv0.GetComponentsInChildren<UnityEngine.UI.Button>()) if (b0 != null && b0.interactable && b0.gameObject.activeInHierarchy) { b0.onClick.Invoke(); break; } } } yield return null; }   // 컷씬 뒤 카드·말풍선은 사람이 누르는 것 대신 닫음
                  if (waited) { Log($"    (이야기 컷씬이 떠서 {sw:0}s 기다림)"); { var fb0 = hub.GetType().GetField("_busy", BF); if (fb0 != null && !(bool)F(hub, "_storyPlaying")) fb0.SetValue(hub, false); } hub.Teleport(home, 180f); yield return new WaitForSeconds(0.6f); } }
                save.stats.stamina = PlayerStats.StatMax;
                // 지금 규칙(끊긴 길 포함)으로 자동이동 — 잠긴 곳은 끊긴 길 무시 경로로 따로 걸어 본다
                bool open = (bool)M(hub, "AutoMoveOpen", sp);
                M(hub, "StartAutoMove", id);
                var pl = F(hub, "_player") as Transform; float t = 0f; float stuckT = 0f; Vector3 last = pl.position; int stalls = 0, jumps = 0; float offRoad = 0f; float diagT = 0f;
                var rt = VillageRoad.Route(save, pl.position, pos, out var why0); bool holdSeen = false;
                while (t < 90f)
                {
                    yield return null; t += Time.deltaTime;
                    var hud = F(hub, "_hud") as VillageHud; if (hud != null && hud.PopupOpen) { if (stalls > 3 && diagT <= 0f) Log("    팝업: " + hud.name); hud.ClosePopup(); }
                    { var mhs = hub.GetType().GetMethod("StoryHoldsDoor", BF); if (mhs != null && (bool)mhs.Invoke(hub, new object[] { pos })) holdSeen = true; }   // 225차: 멈추는 순간의 「이야기 빛」 여부를 기억
                    if (F(hub, "_autoCo") == null && t > 0.5f) break;
                    var mv = pl.position - last; mv.y = 0f;
                    if (mv.magnitude > 3f) jumps++;   // 순간이동(막혀서 문 앞으로)
                    if (mv.sqrMagnitude < 0.0004f) { stuckT += Time.deltaTime; if (stuckT > 0.7f) { stalls++; stuckT = 0f; } } else stuckT = 0f;
                    var dd = pl.position - pos; dd.y = 0f;
                    if (dd.magnitude > 5f && (pl.position - home).magnitude > 5f) { float od = VillageRoadNet.CenterDist(new Vector2(pl.position.x, pl.position.z), out var _); if (od > offRoad) offRoad = od; }
                    diagT -= Time.deltaTime;
                    if (stalls > 3 && diagT <= 0f) { diagT = 10f; var wt = F(hub, "_walkTo"); Log($"    멈춤 진단 t={t:0} 위치 ({pl.position.x:0.0},{pl.position.z:0.0}) busy={F(hub, "_busy")} 잠김={(hud != null && hud.Locked)} 팝업={(hud != null && hud.PopupOpen)} walkTo={wt} 집안={(F(hub, "_interior") as Object != null)}"); }
                    last = pl.position;
                }
                var d = pl.position - pos; d.y = 0f; bool inside = F(hub, "_interior") as Object != null;
                bool arrived = d.magnitude < Mathf.Max(2.2f, rad) || inside;
                // 224차: 이야기 빛이 이 장소 앞이면 게임이 일부러 문 앞에서 멈춘다(213차 설계 — 꼬마 대사 먼저) → 3 m 안이면 도착으로 셈
                bool storyHold = false; { var mh = hub.GetType().GetMethod("StoryHoldsDoor", BF); if (mh != null && !arrived && d.magnitude < rad + 1.5f) storyHold = holdSeen || (bool)mh.Invoke(hub, new object[] { pos }); }
                if (storyHold) arrived = true;
                if (arrived) ok++; else fail++;
                Log($"- {id}: {(open ? "열림" : "잠김(끊긴 길)")} · {(arrived ? "도착" : "!! 못 감")} {t:0.0}s · 남은 거리 {d.magnitude:0.0} m · 멈칫 {stalls}회 · 순간이동 {jumps}회 · 길 가운데서 최대 {offRoad:0.0} m{(inside ? " · 들어감" : "")}{(storyHold ? " · 이야기 빛 앞에서 멈춤(설계)" : "")}");
                if (!arrived || jumps > 0) { var sb2 = new StringBuilder(); if (rt != null) foreach (var q in rt) sb2.Append($"({q.x:0.0},{q.z:0.0}) "); Log($"    경로({why0}): {sb2}"); }
                if (F(hub, "_autoCo") != null) M(hub, "CancelAuto", false);
                if (hub != null) { var hud = F(hub, "_hud") as VillageHud; if (hud != null && hud.PopupOpen) hud.ClosePopup(); }
                // 도착해서 열린 가게·알바·펫 화면 등은 닫고(시험 도구), 막힘 상태를 푼다
                foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (cv != null && cv.isRootCanvas && cv.sortingOrder >= 150 && !cv.name.Contains("Hud") && !cv.name.Contains("Toast") && !cv.name.Contains("Cinematic")) Destroy(cv.gameObject);
                yield return null;
                var fb = hub.GetType().GetField("_busy", BF); if (fb != null) fb.SetValue(hub, false);
                { var hud = F(hub, "_hud") as VillageHud; if (hud != null) hud.Locked = false; }
                if (F(hub, "_interior") as Object != null) { M(hub, "ExitHouse"); yield return new WaitForSeconds(1.2f); fb?.SetValue(hub, false); }
                yield return new WaitForSeconds(0.5f);
            }
            Log($"도착 {ok} · 실패 {fail}");
            VillageHub.DevAutoBubble = false;
        }
    }
}
#endif
