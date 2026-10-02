using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 218차(사용자: 「AAA 급 모바일 게임처럼 부드럽게 점검」): 플레이 중 프로파일러를 잠깐 켜서 프레임당 GC 할당이 많은 곳(마커 이름)을 로그로.
    /// 1) 메뉴 「218 - GC profile start」 → 2~3 초 뒤 2) 「218 - GC profile report」. 결과는 [218] GC top 로그.
    public static class DevMenu218
    {
        [MenuItem("Coast Run/Dev/218 - GC profile start")]
        static void Start() { ProfilerDriver.ClearAllFrames(); ProfilerDriver.enabled = true; Debug.LogWarning("[218] profiler recording…"); }

        [MenuItem("Coast Run/Dev/218 - GC profile report")]
        static void Report()
        {
            ProfilerDriver.enabled = false;
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            var sum = new Dictionary<string, double>(); var cnt = new Dictionary<string, int>(); int frames = 0;
            for (int f = Mathf.Max(first, last - 90); f <= last; f++)
            {
                using (var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnGcMemory, false))
                {
                    if (v == null || !v.valid) continue; frames++;
                    var stack = new Stack<int>(); stack.Push(v.GetRootItemID()); var kids = new List<int>();
                    while (stack.Count > 0)
                    {
                        int id = stack.Pop(); kids.Clear(); v.GetItemChildren(id, kids);
                        foreach (var c in kids)
                        {
                            var nm = v.GetItemName(c);
                            // 자기 자신 GC(자식 제외)만 모은다
                            float self = v.GetItemColumnDataAsFloat(c, HierarchyFrameDataView.columnGcMemory);
                            var gk = new List<int>(); v.GetItemChildren(c, gk); foreach (var g in gk) self -= v.GetItemColumnDataAsFloat(g, HierarchyFrameDataView.columnGcMemory);
                            if (self > 0) { var path = v.GetItemPath(c); string key = nm + "  ←  " + ParentName(v, c); sum[key] = (sum.TryGetValue(key, out var s) ? s : 0) + self; cnt[key] = (cnt.TryGetValue(key, out var n) ? n : 0) + 1; }
                            stack.Push(c);
                        }
                    }
                }
            }
            var sb = new System.Text.StringBuilder($"[218] GC top ({frames} frames, bytes/frame)\n");
            foreach (var kv in sum.OrderByDescending(k => k.Value).Take(25)) sb.AppendLine($"[218]  {kv.Value / Mathf.Max(1, frames):0}  {kv.Key}");
            Debug.LogWarning(sb.ToString());
        }
        /// 218차: 셰이더 첫 사용 끊김(에디터의 하늘색 대기 화면, 기기에선 순간 멈춤) 방지 — 이 에디터 세션에서 쓰인 셰이더 변형을
        /// Assets/_CoastRun/Settings/CoastRun_Warmup.shadervariants 로 저장하고 그래픽 설정 「미리 불러올 셰이더」에 넣는다(부팅 스플래시 동안 컴파일).
        [MenuItem("Coast Run/Dev/218 - Save shader variants → preload")]
        static void SaveVariants()
        {
            var su = typeof(UnityEditor.ShaderUtil);
            var count = su.GetMethod("GetCurrentShaderVariantCollectionVariantCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var save = su.GetMethod("SaveCurrentShaderVariantCollection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            const string path = "Assets/_CoastRun/Settings/CoastRun_Warmup.shadervariants";
            int n = count != null ? (int)count.Invoke(null, null) : -1;
            if (save == null) { Debug.LogWarning("[218] SaveCurrentShaderVariantCollection 없음"); return; }
            save.Invoke(null, new object[] { path }); AssetDatabase.ImportAsset(path);
            var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs); var arr = so.FindProperty("m_PreloadedShaders");
            bool has = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == svc) has = true;
            if (!has && svc != null) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = svc; so.ApplyModifiedProperties(); }
            AssetDatabase.SaveAssets();
            Debug.LogWarning($"[218] shader variants saved: {n} variants, shaders={(svc != null ? svc.shaderCount : -1)}, variants={(svc != null ? svc.variantCount : -1)} → preload={(svc != null)}");
        }
        static string ParentName(HierarchyFrameDataView v, int id)
        {
            var anc = new List<int>(); v.GetItemAncestors(id, anc);
            return string.Join(" < ", anc.Take(3).Select(a => v.GetItemName(a)));
        }
    }
}
