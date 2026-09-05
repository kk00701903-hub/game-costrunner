#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Game 뷰를 세로(720×1280)로 고정하는 메뉴. 마우스로 드롭다운을 못 고를 때를 위한 키보드 경로.
    public static class GameViewAspectMenu
    {
        [MenuItem("Coast Run/Debug/Game view 세로 720x1280 %#&p")]
        public static void SetPortrait() => Select("Portrait 720x1280", 720, 1280);

        private static void Select(string label, int w, int h)
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                var groupType = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });

                int count = (int)groupType.GetType().GetMethod("GetTotalCount").Invoke(groupType, null);
                int found = -1;
                for (int i = 0; i < count; i++)
                {
                    var size = groupType.GetType().GetMethod("GetGameViewSize").Invoke(groupType, new object[] { i });
                    string text = size.GetType().GetProperty("baseText").GetValue(size) as string;
                    if (text == label) { found = i; break; }
                }
                if (found < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var enumType = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
                    var newSize = ctor.Invoke(new object[] { Enum.Parse(enumType, "FixedResolution"), w, h, label });
                    groupType.GetType().GetMethod("AddCustomSize").Invoke(groupType, new[] { newSize });
                    found = (int)groupType.GetType().GetMethod("GetTotalCount").Invoke(groupType, null) - 1;
                }

                var gameViewType = asm.GetType("UnityEditor.GameView");
                var gv = EditorWindow.GetWindow(gameViewType);
                var m = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m.Invoke(gv, new object[] { found, null });
                gv.Repaint();
                Debug.Log("[Coast Run] Game view → " + label);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Coast Run] Game view aspect reflection failed: " + e.Message);
            }
        }
    }
}
#endif
