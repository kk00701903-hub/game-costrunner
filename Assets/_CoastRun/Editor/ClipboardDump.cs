using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 77차: 클로드 브라우저 패널(같은 PC)에서 클립보드로 넘긴 텍스트를 파일로 떨어뜨린다 — 브릿지에 파일 쓰기 통로가 없을 때의 우회.
    /// 원격 `menu Coast Run/Dev/Clipboard - dump` → Tools/_clip/clip.txt
    public static class ClipboardDump
    {
        [MenuItem("Coast Run/Dev/Clipboard - dump")]
        public static void Dump()
        {
            string s = GUIUtility.systemCopyBuffer ?? "";
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "_clip");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "clip.txt");
            File.WriteAllText(path, s);
            Debug.LogWarning($"[Clip] {s.Length} chars → {path}");
        }
    }
}
