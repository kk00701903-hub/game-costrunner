#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Mixamo FBX pipeline. Drop files into Assets/Resources/CoastRun/Rig/:
    ///   Skater.fbx          — the character, "with skin", T-pose (any Mixamo body)
    ///   Anim_Skate.fbx      — riding loop (one foot on the board, glide)
    ///   Anim_Push.fbx       — kick-off with the back foot (optional, loops into Skate)
    ///   Anim_Jump.fbx       — hop over an obstacle
    ///   Anim_Hit.fbx        — stumble / impact
    ///   Anim_Collect.fbx    — grab (played on an upper-body layer while riding)
    /// Every FBX is imported as Humanoid so the clips retarget onto the Skater avatar.
    /// "Coast Run/Art/Build Skater animator" then writes SkaterAnimator.controller
    /// next to them; CoastPlayerVisual loads both from Resources at runtime.
    public class MixamoImportSettings : AssetPostprocessor
    {
        public const string Folder = "Assets/Resources/CoastRun/Rig/";

        private void OnPreprocessModel()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Folder) || !path.EndsWith(".fbx"))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;          // Mixamo exports in cm; Unity's FBX reader converts
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.resampleCurves = true;

            string file = Path.GetFileNameWithoutExtension(path);
            bool isClip = file.StartsWith("Anim_");
            if (isClip)
            {
                // Clip files: the skin (if any) is unused; keep only the animation.
                importer.importBlendShapes = false;
                var clips = importer.defaultClipAnimations;
                for (int i = 0; i < clips.Length; i++)
                {
                    var c = clips[i];
                    c.name = file.Substring("Anim_".Length);
                    bool loop = c.name == "Skate" || c.name == "Push";
                    c.loopTime = loop;
                    c.loopPose = loop;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;      // the controller moves the player
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                    c.mirror = false;
                    clips[i] = c;
                }
                importer.clipAnimations = clips;
            }
        }

        [MenuItem("Coast Run/Art/Build Skater animator (Mixamo clips)")]
        public static void BuildAnimator()
        {
            // Mixamo embeds the textures inside the FBX; until they are extracted the
            // prefab's materials have no maps and the skater renders flat white.
            var skaterImporter = AssetImporter.GetAtPath(Folder + "Skater.fbx") as ModelImporter;
            if (skaterImporter != null)
            {
                string texDir = Folder + "Textures";
                if (!AssetDatabase.IsValidFolder(texDir.TrimEnd('/')))
                    AssetDatabase.CreateFolder(Folder.TrimEnd('/'), "Textures");
                if (skaterImporter.ExtractTextures(texDir))
                {
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(Folder + "Skater.fbx", ImportAssetOptions.ForceUpdate);
                    Debug.Log("[Mixamo] Extracted skater textures → " + texDir);
                }
            }

            AnimationClip Clip(string name)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath(Folder + "Anim_" + name + ".fbx");
                foreach (var o in all)
                    if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                        return c;
                Debug.LogWarning($"[Mixamo] Anim_{name}.fbx not found under {Folder}");
                return null;
            }

            var skate = Clip("Skate");
            if (skate == null)
            {
                Debug.LogError("[Mixamo] Need at least Anim_Skate.fbx.");
                return;
            }
            var push = Clip("Push");
            var jump = Clip("Jump");
            var hit = Clip("Hit");
            var collect = Clip("Collect");

            string ctrlPath = Folder + "SkaterAnimator.controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl != null)
                AssetDatabase.DeleteAsset(ctrlPath);
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Collect", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Push", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var sm = ctrl.layers[0].stateMachine;
            var sSkate = sm.AddState("Skate");
            sSkate.motion = skate;
            sm.defaultState = sSkate;

            if (push != null)
            {
                var sPush = sm.AddState("Push");
                sPush.motion = push;
                var t = sSkate.AddTransition(sPush);
                t.AddCondition(AnimatorConditionMode.If, 0, "Push");
                t.hasExitTime = false; t.duration = 0.12f;
                var back = sPush.AddTransition(sSkate);
                back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.15f;
            }
            if (jump != null)
            {
                var sJump = sm.AddState("Jump");
                sJump.motion = jump;
                var t = sm.AddAnyStateTransition(sJump);
                t.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
                var land = sJump.AddTransition(sSkate);
                land.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
                land.hasExitTime = true; land.exitTime = 0.6f; land.duration = 0.15f;
                var landLate = sJump.AddTransition(sSkate);
                landLate.hasExitTime = true; landLate.exitTime = 0.98f; landLate.duration = 0.1f;
            }
            if (hit != null)
            {
                var sHit = sm.AddState("Hit");
                sHit.motion = hit;
                var t = sm.AddAnyStateTransition(sHit);
                t.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
                var back = sHit.AddTransition(sSkate);
                back.hasExitTime = true; back.exitTime = 0.7f; back.duration = 0.2f;
            }

            if (collect != null)
            {
                // Upper-body layer so a grab plays over the riding legs.
                var mask = new AvatarMask();
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                    mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
                mask.name = "UpperBody";
                AssetDatabase.AddObjectToAsset(mask, ctrl);

                var layer = new AnimatorControllerLayer
                {
                    name = "UpperBody",
                    defaultWeight = 1f,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    avatarMask = mask,
                    stateMachine = new AnimatorStateMachine { name = "UpperBody", hideFlags = HideFlags.HideInHierarchy },
                };
                AssetDatabase.AddObjectToAsset(layer.stateMachine, ctrl);
                var idle = layer.stateMachine.AddState("Empty");
                layer.stateMachine.defaultState = idle;
                var sCollect = layer.stateMachine.AddState("Collect");
                sCollect.motion = collect;
                var t = idle.AddTransition(sCollect);
                t.AddCondition(AnimatorConditionMode.If, 0, "Collect");
                t.hasExitTime = false; t.duration = 0.08f;
                var back = sCollect.AddTransition(idle);
                back.hasExitTime = true; back.exitTime = 0.8f; back.duration = 0.15f;
                ctrl.AddLayer(layer);
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mixamo] Wrote " + ctrlPath);
        }
    }
}
#endif
