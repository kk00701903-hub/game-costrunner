using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 143차: 믹사모(8등신) 클립을 2.5등신 치비 Humanoid 에 얹을 때의 교정 — 컨트롤러·클립 임포트 설정을 일괄 적용.
    /// 메뉴: Coast Run/Dev/Rig - Chibi retarget fix
    ///  1) RunnerAnimator/SkaterAnimator: 모든 레이어 IK Pass ON, 모든 스테이트 Foot IK ON(발이 땅을 뚫거나 뜨는 것 방지)
    ///  2) Rig/*.fbx 애니 임포트: Humanoid, Root Transform Rotation/Position(Y)/Position(XZ) Bake Into Pose,
    ///     Loop Time, 'Based Upon' Original — 보폭(루트 모션)은 코드에서 속도로 맞추므로 루트 이동은 굽는다.
    public static class ChibiRetargetFix
    {
        [MenuItem("Coast Run/Dev/Rig - Chibi retarget fix")]
        static void Run()
        {
            int states = 0, clips = 0;
            foreach (var path in new[] { "Assets/Resources/CoastRun/Rig/RunnerAnimator.controller", "Assets/Resources/CoastRun/Rig/SkaterAnimator.controller" })
            {
                var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (ac == null) continue;
                foreach (var layer in ac.layers)
                {
                    layer.iKPass = true;
                    foreach (var st in layer.stateMachine.states) { st.state.iKOnFeet = true; states++; }
                }
                // layers 는 복사본 — 다시 써 줘야 저장된다
                var ls = ac.layers; for (int i = 0; i < ls.Length; i++) ls[i].iKPass = true; ac.layers = ls;
                EditorUtility.SetDirty(ac);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources/CoastRun/Rig" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(p) as ModelImporter; if (imp == null) continue;
                bool changed = false;
                if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; changed = true; }
                var arr = imp.clipAnimations; if (arr == null || arr.Length == 0) arr = imp.defaultClipAnimations;
                foreach (var c in arr)
                {
                    c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                    c.loopTime = true; c.loopPose = true; clips++;
                }
                if (arr.Length > 0) { imp.clipAnimations = arr; changed = true; }
                if (changed) { EditorUtility.SetDirty(imp); imp.SaveAndReimport(); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ChibiRetargetFix] Foot IK on {states} states, {clips} clips re-imported (root motion baked, loop)");
        }
    }
}
