#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CoastRun.Village;

namespace CoastRun.DevQA
{
    /// 222차: 221차에 새로 넣은 안내(땅 메뉴 한 줄·자기 전 밥 안내 2종·스트레스 안내)가
    /// 4개 언어(ko·en·ja·es)에서 칸을 넘치지 않는지 — 실제 마을 HUD 팝업에 띄워 글자 넘침 검사 + 스크린샷.
    /// 결과 Builds/qa/popup_fit_222.txt, 스크린샷 Builds/qa/fit222/. 언어는 끝나면 원래대로.
    public class PopupFitQA222 : MonoBehaviour
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        [MenuItem("Coast Run/QA/222 - New popup fit (4 langs)")]
        public static void Begin() { if (!Application.isPlaying) { Debug.LogError("[Fit222] 플레이 중에만"); return; } var go = new GameObject("PopupFitQA222"); DontDestroyOnLoad(go); go.AddComponent<PopupFitQA222>(); }

        readonly StringBuilder _log = new StringBuilder(); int _shot;
        static object F(object o, string n) { if (o == null) return null; var f = o.GetType().GetField(n, BF); return f != null ? f.GetValue(o) : null; }
        static object M(object o, string n, params object[] a) { if (o == null) return null; var m = o.GetType().GetMethod(n, BF, null, Type.EmptyTypes, null); return m != null ? m.Invoke(o, a) : null; }

        IEnumerator Start()
        {
            if (_snapOnly) { yield return SnapCo(); yield break; }
            if (_near) { Directory.CreateDirectory("Builds/qa/fit222"); yield return NearCo(); yield break; }
            if (_spots) { Directory.CreateDirectory("Builds/qa/fit222"); yield return SpotsCo(); yield break; }
            if (_bus) { Directory.CreateDirectory("Builds/qa/fit222"); yield return BusCo(); yield break; }
            string orig = Loc.Lang; string savedPref = PlayerPrefs.GetString(Loc.PrefKey, "");
            float w = 0f; while (VillageHub.I == null && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
            var hub = VillageHub.I; var hud = F(hub, "_hud") as VillageHud;
            if (hub == null || hud == null) { Debug.LogError("[Fit222] 마을 없음"); yield break; }
            Directory.CreateDirectory("Builds/qa/fit222");
            _log.AppendLine("=== 222 새 안내 칸 넘침 검사 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " ===");
            int bad = 0;
            foreach (var lang in Loc.Langs)
            {
                Loc.SetLang(lang);
                _log.AppendLine("\n## " + lang);
                // ① 땅 메뉴(실제 메뉴)
                hud.ClosePopup(); M(hub, "LandMenu"); yield return new WaitForSecondsRealtime(0.8f);
                bad += Audit(lang + "_land"); yield return ShotCo(lang + "_land");
                // ② 밥 안내(먹을 것 있음) — 221차 SleepBed 와 같은 문장
                hud.ClosePopup();
                hud.Choice(Loc.T("🍚 꼬마", "🍚 Kid"), Loc.T("누나, 밥은? 식탁에 먹을 거 있어. 안 먹고 자면 한 주 내내 배고파.", "Sis, did you eat? There's food on the table. Skip it and you'll be hungry all week."),
                    new (string, Color, Action)[] { (Loc.T("🍚 밥 먹을래", "🍚 Eat first"), new Color(0.95f, 0.70f, 0.30f), null), (Loc.T("💤 그냥 잘래", "💤 Just sleep"), new Color(0.60f, 0.52f, 0.92f), null) });
                yield return new WaitForSecondsRealtime(0.8f);
                bad += Audit(lang + "_meal"); yield return ShotCo(lang + "_meal");
                // ③ 밥 안내(먹을 것 없음)
                hud.ClosePopup();
                hud.Choice(Loc.T("🍚 꼬마", "🍚 Kid"), Loc.T("누나, 집에 먹을 게 없어. 가게에서 밥거리를 사 오거나 카페 브런치를 먹자. 안 먹고 자면 한 주 내내 배고파.", "Sis, there's no food at home. Buy some at the shop or have café brunch. Skip it and you'll be hungry all week."),
                    new (string, Color, Action)[] { (Loc.T("알았어", "Okay"), new Color(0.95f, 0.70f, 0.30f), null), (Loc.T("💤 그냥 잘래", "💤 Just sleep"), new Color(0.60f, 0.52f, 0.92f), null) });
                yield return new WaitForSecondsRealtime(0.8f);
                bad += Audit(lang + "_nofood"); yield return ShotCo(lang + "_nofood");
                // ④ 스트레스 안내(말풍선 — 한 글자씩 나오므로 기다림)
                hud.ClosePopup();
                hud.Bubble(Loc.T("꼬마", "Kid"), Loc.T("누나, 요즘 안 웃어. 오름 산책이나 바다 수영 가자. 카페 브런치도 좋아.", "Sis, you don't smile lately. Let's walk the oreum or swim in the sea. Café brunch works too."));
                yield return new WaitForSecondsRealtime(4.5f);
                bad += Audit(lang + "_stress"); yield return ShotCo(lang + "_stress");
                hud.ClosePopup();
            }
            if (string.IsNullOrEmpty(savedPref)) { Loc.SetLang(orig); PlayerPrefs.DeleteKey(Loc.PrefKey); PlayerPrefs.Save(); } else Loc.SetLang(savedPref);
            _log.AppendLine($"\n=== 끝 · 넘침 {bad}건 · 언어 원래대로({orig}) ===");
            File.WriteAllText("Builds/qa/popup_fit_222.txt", _log.ToString());
            Debug.LogWarning("[Fit222] 끝 · 넘침 " + bad);
            Destroy(gameObject);
        }

        int Audit(string tag)
        {
            int n = 0, total = 0;
            foreach (var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || !t.gameObject.activeInHierarchy) continue;
                var r = t.rectTransform.rect; total++;
                if (r.width < 4f || r.height < 4f || t.resizeTextForBestFit) continue;
                bool wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap;
                float pw = t.preferredWidth, ph = t.preferredHeight;
                bool over = wrap ? ph > r.height + 2f : (pw > r.width + 2f || (t.verticalOverflow == VerticalWrapMode.Truncate && ph > r.height + 2f));
                if (!over) continue;
                if (t.GetComponentInParent<LayoutGroup>() != null && wrap && ph <= r.height + 40f) continue;
                string txt = t.text.Replace("\n", "⏎"); if (txt.Length > 50) txt = txt.Substring(0, 50) + "…";
                _log.AppendLine($"  !! {tag} {t.name} [{txt}] need {pw:0}x{ph:0} > box {r.width:0}x{r.height:0}");
                n++;
            }
            _log.AppendLine($"  {tag}: 글자 {total}개 중 넘침 {n}");
            return n;
        }


        /// 일본어 줄바꿈 실험: 띄어쓰기 없는 문장이 Text 에서 줄바꿈되는지, 글자 사이에 U+200B 를 넣으면 되는지
        [MenuItem("Coast Run/QA/222 - JA wrap probe")]
        public static void JaWrapProbe()
        {
            if (!Application.isPlaying) { Debug.LogError("[JaWrap] 플레이 중에만"); return; }
            var cv = new GameObject("JaWrapCv", typeof(Canvas)); var go = new GameObject("T", typeof(RectTransform), typeof(Text)); go.transform.SetParent(cv.transform, false);
            var t = go.GetComponent<Text>(); var any = FindObjectsByType<Text>(FindObjectsSortMode.None); foreach (var a in any) if (a != t && a.font != null) { t.font = a.font; break; }
            t.fontSize = 22; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; ((RectTransform)t.transform).sizeDelta = new Vector2(400f, 100f);
            string ja = "お姉ちゃん、最近笑わないね。オルム散歩か海で泳ごう。カフェのブランチもいいよ。";
            var sb = new StringBuilder(); foreach (var ch in ja) { sb.Append(ch); sb.Append('\u200B'); }
            foreach (var (name, txt) in new[] { ("raw", ja), ("zwsp", sb.ToString()), ("space", string.Join(" ", ja.ToCharArray())) })
            {
                t.text = txt; Canvas.ForceUpdateCanvases();
                var gen = t.cachedTextGenerator; var set = t.GetGenerationSettings(new Vector2(400f, 100f)); gen.Populate(txt, set);
                Debug.LogWarning($"[JaWrap] {name}: lines {gen.lineCount} · preferred {t.preferredWidth:0}x{t.preferredHeight:0} · font {(t.font != null ? t.font.name : "null")}");
            }
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 13; t.resizeTextMaxSize = 22; t.verticalOverflow = VerticalWrapMode.Truncate;
            foreach (var (name, txt) in new[] { ("bf_ja", ja), ("bf_ko", "누나, 요즘 안 웃어. 오름 산책이나 바다 수영 가자. 카페 브런치도 좋아."), ("bf_en", "Sis, you don't smile lately. Let's walk the oreum or swim in the sea. Café brunch works too.") })
            {
                t.text = txt; var gen = new TextGenerator(); var set = t.GetGenerationSettings(new Vector2(400f, 100f)); gen.Populate(txt, set);
                Debug.LogWarning($"[JaWrap] {name}: bestfit size {gen.fontSizeUsedForBestFit} lines {gen.lineCount}");
            }
            Destroy(cv);
        }


        /// 지금 화면 한 장 + 마을 상태(busy·이야기·실내)·떠 있는 캔버스 — Builds/qa/snap222_*.jpg / snap222.txt
        [MenuItem("Coast Run/QA/222 - Snap state")]
        public static void Snap() { if (!Application.isPlaying) return; var go = new GameObject("Snap222"); go.AddComponent<PopupFitQA222>()._snapOnly = true; }
        bool _snapOnly;
        void Awake() { if (_snapOnly) { } }
        IEnumerator SnapCo()
        {
            var sb = new StringBuilder(DateTime.Now.ToString("HH:mm:ss") + "\n");
            var hub = VillageHub.I;
            if (hub != null) foreach (var n in new[] { "_busy", "_storyPlaying", "_autoCo", "_interior", "_walkTo" }) sb.Append(n + "=" + (F(hub, n) ?? "null") + "\n");
            foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (cv.isRootCanvas && cv.isActiveAndEnabled) sb.Append("canvas " + cv.name + " order " + cv.sortingOrder + "\n");
            sb.Append("scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\n");
            File.WriteAllText("Builds/qa/snap222.txt", sb.ToString());
            Directory.CreateDirectory("Builds/qa/fit222");
            yield return ShotCo("snap_" + DateTime.Now.ToString("HHmmss"));
            Destroy(gameObject);
        }


        /// 버스 정류장 자동이동 재현: 집 앞 → StartAutoMove("bus") → 1·4·10초 뒤 상태·화면
        [MenuItem("Coast Run/QA/222 - Bus auto-move probe")]
        public static void BusProbe() { if (!Application.isPlaying) return; var go = new GameObject("Bus222"); go.AddComponent<PopupFitQA222>()._bus = true; }
        bool _bus;
        IEnumerator BusCo()
        {
            var hub = VillageHub.I; if (hub == null) yield break;
            var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 5.6f)) : new Vector3(0f, 0f, 30f);
            // 끊긴 길(유료)을 이 시험 동안만 이어 둔다 — Road QA 와 같은 방식(세이브는 플레이 끝나면 복원)
            var save = GameManager.I != null ? GameManager.I.Save : null;
            if (save != null) { if (save.roadCells == null) save.roadCells = new System.Collections.Generic.List<int>(); foreach (var g in VillageWorld.RoadGaps) for (float dx = -4f; dx <= 4f; dx += 1f) for (float dz = -4f; dz <= 4f; dz += 1f) { int k = VillageRoad.KeyAt(new Vector3(g.x + dx, 0f, g.y + dz)); if (!save.roadCells.Contains(k)) save.roadCells.Add(k); } }
            hub.Teleport(home, 180f); yield return new WaitForSeconds(0.8f);
            var sb = new StringBuilder("bus probe " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            { var sp = (F(hub, "_spots") as IList); foreach (var o in sp) if ((string)F(o, "id") == "bus") { var pp = (Vector3)F(o, "pos"); sb.Append($"bus spot ({pp.x:0.0},{pp.z:0.0}) r={F(o, "radius")}\n"); var rt = VillageRoad.Route(save, (F(hub, "_player") as Transform).position, pp, out var why); if (rt != null && rt.Count > 0) sb.Append($"route {rt.Count} pts, last ({rt[rt.Count - 1].x:0.0},{rt[rt.Count - 1].z:0.0}) why={why}\n"); else sb.Append("route null why=" + why + "\n"); } }
            sb.Append("before busy=" + F(hub, "_busy") + " story=" + F(hub, "_storyPlaying") + "\n");
            hub.GetType().GetMethod("StartAutoMove", BF).Invoke(hub, new object[] { "bus" });
            foreach (var w in new[] { 2f, 6f, 12f, 20f })
            {
                yield return new WaitForSeconds(w);
                var pl = F(hub, "_player") as Transform;
                sb.Append($"t+{w}: busy={F(hub, "_busy")} auto={(F(hub, "_autoCo") != null)} pos=({pl.position.x:0.0},{pl.position.z:0.0}) walkTo={F(hub, "_walkTo")}\n");
                foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (cv.isRootCanvas && cv.isActiveAndEnabled && cv.sortingOrder >= 100) sb.Append("   canvas " + cv.name + " " + cv.sortingOrder + "\n");
                var hud = F(hub, "_hud") as VillageHud; if (hud != null && hud.PopupOpen) { var pop = F(hud, "_popup") as RectTransform; if (pop != null) { var tsb = new StringBuilder(); foreach (var t in pop.GetComponentsInChildren<Text>()) tsb.Append(t.text.Replace("\n", " ") + " | "); sb.Append("   popup: " + tsb + "\n"); } }
                sb.Append($"   storyHold={hub.GetType().GetMethod("StoryHoldsDoor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(hub, new object[] { (Vector3)F(hub, "_storyPos") })} storyPos={F(hub, "_storyPos")}\n");
            }
            File.WriteAllText("Builds/qa/bus_probe_222.txt", sb.ToString());
            Destroy(gameObject);
        }


        /// 카메라 코앞 물체 조사: 지정 지점에서 네 방향으로 서 보고 카메라 2.5 m 안 렌더러 목록 + 스크린샷 — Builds/qa/near_cam_222.txt
        [MenuItem("Coast Run/QA/222 - Near-camera probe (-7,26)")]
        public static void NearCam() { if (!Application.isPlaying) return; var go = new GameObject("Near222"); go.AddComponent<PopupFitQA222>()._near = true; }
        bool _near;
        IEnumerator NearCo()
        {
            var hub = VillageHub.I; if (hub == null) yield break;
            var cam = F(hub, "_cam") as Camera; var pl = F(hub, "_player") as Transform;
            var sb = new StringBuilder("near-cam probe " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var p = VillageWorld.Ground(-7f, 26f);
            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                hub.Teleport(p, yaw); for (int k = 0; k < 40; k++) yield return null;
                var cp = cam.transform.position;
                sb.Append($"yaw {yaw}: cam ({cp.x:0.0},{cp.y:0.0},{cp.z:0.0}) player ({pl.position.x:0.0},{pl.position.y:0.0},{pl.position.z:0.0})\n");
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(pl)) continue;
                    var b = r.bounds; if (b.size.x > 60f || b.size.z > 60f) continue;
                    float d = Mathf.Sqrt(b.SqrDistance(cp)); if (d > 2.5f) continue;
                    var vp = cam.WorldToViewportPoint(b.center);
                    string path = r.name; var t = r.transform.parent; int dd = 0; while (t != null && dd++ < 4) { path = t.name + "/" + path; t = t.parent; }
                    sb.Append($"   {d:0.00} m  {path}  size ({b.size.x:0.0},{b.size.y:0.0},{b.size.z:0.0})  vp ({vp.x:0.00},{vp.y:0.00},{vp.z:0.0})\n");
                }
                foreach (var v in new[] { new Vector2(0.85f, 0.12f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.35f) })
                {
                    var ray = cam.ViewportPointToRay(new Vector3(v.x, v.y, 0f)); string hitName = "-"; float best = 999f;
                    foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    {
                        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                        var b = r.bounds; if (b.size.x > 60f || b.size.z > 60f) continue;
                        if (b.IntersectRay(ray, out float hd) && hd < best && hd > 0.01f) { best = hd; string path = r.name; var t = r.transform.parent; int dd = 0; while (t != null && dd++ < 3) { path = t.name + "/" + path; t = t.parent; } hitName = path + $" size ({b.size.x:0.0},{b.size.y:0.0},{b.size.z:0.0})"; }
                    }
                    sb.Append($"   ray vp({v.x},{v.y}) first box {best:0.0} m: {hitName}\n");
                }
                yield return ShotCo("near_" + yaw);
            }
            File.WriteAllText("Builds/qa/near_cam_222.txt", sb.ToString());
            Destroy(gameObject);
        }


        /// 225차: 엄마 집 마루·송전탑 밑·버스 정류장에서 네 방향 스크린샷(가림 조각 숨김이 어색하지 않은지 눈으로 확인)
        [MenuItem("Coast Run/QA/222 - Spot shots (mom, tower, bus)")]
        public static void SpotShots() { if (!Application.isPlaying) return; var go = new GameObject("Spot222"); go.AddComponent<PopupFitQA222>()._spots = true; }
        bool _spots;
        IEnumerator SpotsCo()
        {
            var hub = VillageHub.I; if (hub == null) yield break;
            foreach (var (nm, x, z) in new[] { ("mom", 1f, -18f), ("tower", 12f, 38f), ("bus", 67f, 8f) })
                foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
                {
                    hub.Teleport(VillageWorld.Ground(x, z), yaw); for (int k = 0; k < 40; k++) yield return null;
                    yield return ShotCo($"spot_{nm}_{yaw}");
                }
            Destroy(gameObject);
        }

        IEnumerator ShotCo(string tag)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) yield break;
            int w = tex.width / 2, h = tex.height / 2;
            var rt = RenderTexture.GetTemporary(w, h); Graphics.Blit(tex, rt);
            var small = new Texture2D(w, h, TextureFormat.RGB24, false); var prev = RenderTexture.active; RenderTexture.active = rt;
            small.ReadPixels(new Rect(0, 0, w, h), 0, 0); small.Apply(); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes($"Builds/qa/fit222/{++_shot:00}_{tag}.jpg", small.EncodeToJPG(85));
            Destroy(tex); Destroy(small);
        }
    }
}
#endif
