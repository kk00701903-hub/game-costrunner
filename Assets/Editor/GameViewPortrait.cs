using UnityEditor;
using UnityEngine;

/// Sets Game View to a custom portrait resolution for HUD testing.
///
/// Lifted out of PlayTestTools when the A-0347 tree was removed. Nothing here
/// touched that tree; it was only sharing a file with it. CoastRunMenu is the
/// live caller.
internal static class GameViewPortrait
{
    public static void Set(int width, int height)
    {
        var assembly = typeof(EditorWindow).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
            return;

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        gameView.Show();

        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singletonType = assembly.GetType("UnityEditor.ScriptableSingleton`1");
        if (sizesType == null || singletonType == null)
            return;

        var singleton = singletonType.MakeGenericType(sizesType);
        var instanceProp = singleton.GetProperty("instance");
        object sizesInstance = instanceProp?.GetValue(null);
        if (sizesInstance == null)
            return;

        var getGroup = sizesType.GetMethod("GetGroup");
        if (getGroup == null)
            return;

        // GameViewSizeGroupType.Standalone = 0
        object group = getGroup.Invoke(sizesInstance, new object[] { 0 });
        if (group == null)
            return;

        var groupType = group.GetType();
        var getBuiltin = groupType.GetMethod("GetBuiltinCount");
        var getCustom = groupType.GetMethod("GetCustomCount");

        if (getBuiltin == null || getCustom == null)
            return;

        string label = width + "x" + height + " Portrait";
        if (!TryAddCustomSize(assembly, group, groupType, width, height, label))
            return;

        int total = (int)getBuiltin.Invoke(group, null) + (int)getCustom.Invoke(group, null) - 1;
        var setSize = gameViewType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (setSize == null)
            return;

        try
        {
            if (setSize.GetParameters().Length == 2)
                setSize.Invoke(gameView, new object[] { total, null });
            else
                setSize.Invoke(gameView, new object[] { total });
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Game View size select skipped — " + ex.Message);
        }
    }

    private static bool TryAddCustomSize(System.Reflection.Assembly assembly, object group, System.Type groupType, int width, int height, string label)
    {
        var addCustomMethods = groupType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        System.Type sizeType = assembly.GetType("UnityEditor.GameViewSizeType");
        object enumFixed = sizeType != null ? System.Enum.ToObject(sizeType, 1) : null;

        for (int i = 0; i < addCustomMethods.Length; i++)
        {
            if (addCustomMethods[i].Name != "AddCustomSize")
                continue;

            var p = addCustomMethods[i].GetParameters();
            try
            {
                if (p.Length == 4 && sizeType != null && p[0].ParameterType == sizeType)
                    addCustomMethods[i].Invoke(group, new object[] { enumFixed, width, height, label });
                else if (p.Length == 4 && p[0].ParameterType == typeof(int))
                    addCustomMethods[i].Invoke(group, new object[] { 1, width, height, label });
                else
                    continue;

                return true;
            }
            catch (System.Exception)
            {
                // try next overload
            }
        }

        return false;
    }
}
