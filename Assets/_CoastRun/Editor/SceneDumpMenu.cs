using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoastRun.EditorTools
{
    /// Play-mode diagnostics: dumps everything that could paint the screen a flat colour
    /// (cameras, overlay canvases, huge renderers, particle systems, volume grades)
    /// to Tools/scene_dump.txt so a rendering bug can be read instead of guessed at.
    public static class SceneDumpMenu
    {
        [MenuItem("Coast Run/Debug/Dump scene (play mode) %#&d")]
        public static void Dump()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"time {Time.time:F1} scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            var sm = StageManager.Instance;
            if (sm != null)
                sb.AppendLine($"stage {sm.StageIndex} chapter {sm.ChapterIndex} progress {sm.StageProgress01:F2} current={(sm.Current != null ? sm.Current.stageIndex.ToString() : "null")} managers={Object.FindObjectsByType<StageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length} devPref={PlayerPrefs.GetInt(GameSession.DevStartStageKey, 0)}");
            var pl = Object.FindAnyObjectByType<PlayerController>();
            if (pl != null)
                sb.AppendLine($"player z={pl.PathDistance:F1} speed={pl.Speed:F1} lane={pl.Lane}");
            var obstacles = GameObject.Find("Obstacles");
            if (obstacles != null)
            {
                sb.AppendLine("== obstacles");
                foreach (Transform c in obstacles.transform)
                    sb.AppendLine($"   {c.name} z={DownhillPath.DistanceAlong(c.position):F1} x={c.position.x:F1}");
            }

            sb.AppendLine("== cameras");
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var data = c.GetComponent<UniversalAdditionalCameraData>();
                sb.AppendLine($"{c.name} en={c.enabled && c.gameObject.activeInHierarchy} depth={c.depth} rect={c.rect} clear={c.clearFlags} bg={c.backgroundColor} fov={c.fieldOfView:F1} type={(data != null ? data.renderType.ToString() : "-")} post={(data != null && data.renderPostProcessing)} mask={c.cullingMask}");
            }

            sb.AppendLine("== volumes");
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.AppendLine($"{v.name} global={v.isGlobal} weight={v.weight} profile={(v.profile != null ? v.profile.name : "null")}");
                if (v.profile == null) continue;
                foreach (var comp in v.profile.components)
                {
                    sb.Append($"   {comp.GetType().Name} active={comp.active}");
                    if (comp is ColorAdjustments ca)
                        sb.Append($" filter={ca.colorFilter.value} sat={ca.saturation.value} exposure={ca.postExposure.value} contrast={ca.contrast.value}");
                    if (comp is Bloom b)
                        sb.Append($" intensity={b.intensity.value} threshold={b.threshold.value}");
                    if (comp is Vignette vg)
                        sb.Append($" vig={vg.intensity.value} color={vg.color.value}");
                    if (comp is WhiteBalance wb)
                        sb.Append($" temp={wb.temperature.value} tint={wb.tint.value}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("== canvases / big images");
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.AppendLine($"{cv.name} mode={cv.renderMode} order={cv.sortingOrder} active={cv.gameObject.activeInHierarchy} cam={(cv.worldCamera != null ? cv.worldCamera.name : "-")}");
                foreach (var g in cv.GetComponentsInChildren<Graphic>(true))
                {
                    var rt = g.rectTransform;
                    if (rt.rect.width * rt.rect.height < 400 * 400) continue;
                    var cg = g.GetComponentInParent<CanvasGroup>();
                    sb.AppendLine($"   {Path(g.transform)} {rt.rect.width:F0}x{rt.rect.height:F0} color={g.color} cgAlpha={(cg != null ? cg.alpha : 1f):F2} active={g.gameObject.activeInHierarchy} en={g.enabled} type={g.GetType().Name}");
                }
            }

            sb.AppendLine("== particle systems");
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var r = ps.GetComponent<ParticleSystemRenderer>();
                sb.AppendLine($"{Path(ps.transform)} alive={ps.particleCount} playing={ps.isPlaying} active={ps.gameObject.activeInHierarchy} size={ps.main.startSize.constantMax:F2} bounds={r.bounds.size} mat={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "null")} mode={r.renderMode}");
            }

            sb.AppendLine("== huge renderers (extent > 80)");
            var cam = Camera.main;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var e = r.bounds.extents;
                bool near = cam != null && (r.bounds.center - cam.transform.position).magnitude < 3f;
                if (e.x < 80f && e.y < 80f && e.z < 80f && !near) continue;
                var m = r.sharedMaterial;
                string col = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-";
                sb.AppendLine($"{Path(r.transform)} {r.GetType().Name} bounds={r.bounds} near={near} shader={(m != null ? m.shader.name : "null")} color={col} queue={(m != null ? m.renderQueue : 0)}");
            }

            sb.AppendLine("== render settings");
            sb.AppendLine($"fog={RenderSettings.fog} fogColor={RenderSettings.fogColor} mode={RenderSettings.fogMode} start={RenderSettings.fogStartDistance} end={RenderSettings.fogEndDistance} density={RenderSettings.fogDensity} ambient={RenderSettings.ambientLight} skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "null")}");
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                sb.AppendLine($"light {l.name} type={l.type} color={l.color} intensity={l.intensity} en={l.enabled}");

            string path = System.IO.Path.Combine(Application.dataPath, "..", "Tools", "scene_dump.txt");
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Scene dump → " + path);
        }

        // ── Pause when an oncoming car is close: lets a screenshot catch the moment ──
        private static bool _carWatch;

        [MenuItem("Coast Run/Debug/Pause when a car is 14 m out (toggle)")]
        public static void ToggleCarWatch()
        {
            _carWatch = !_carWatch;
            EditorApplication.update -= CarWatch;
            if (_carWatch)
                EditorApplication.update += CarWatch;
            Debug.Log("Car watch " + (_carWatch ? "on" : "off"));
        }

        private static void CarWatch()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;
            var pl = Object.FindAnyObjectByType<PlayerController>();
            if (pl == null) return;
            foreach (var car in Object.FindObjectsByType<OncomingCar>(FindObjectsSortMode.None))
            {
                float d = car.PathZ - pl.PathDistance;
                if (d > 0f && d < 14f)
                {
                    EditorApplication.isPaused = true;
                    Debug.Log($"Car watch: paused, car {d:F1} m ahead in lane {car.Lane}");
                    return;
                }
            }
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                s = t.name + "/" + s;
            }
            return s;
        }
    }
}
