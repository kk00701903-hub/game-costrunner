using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 47차: Blender 를 백그라운드로 돌려 펫 4종 FBX 를 뽑는다(Tools/blender/build_pets.bat → pet_kit.py → Resources/CoastRun/Pet_*.fbx).
    /// 원격(unity_cmd "menu Coast Run/Build Pets 3D (Blender)")에서도 호출 가능. 로그: Tools/blender/pet_log.txt
    public static class BlenderKitMenu
    {
        // 66차: 펫 Blender 키트(Pet_*.fbx)는 삭제됨 — 펫은 Kling 뒷모습 그림(Obs_Pet_*)만 쓴다.

        /// 59차: 도로변 소품 6종(Kerb_*.fbx) — Tools/blender/build_kerb.bat → kerb_kit.py. 로그: kerb_log.txt
        [MenuItem("Coast Run/Build Kerb Props 3D (Blender)")]
        public static void BuildKerb() => RunBat("build_kerb.bat", "kerb_log.txt");

        /// 64차: 제주 집 4종(JHouse_Thatch_A/B · JHouse_Tile_A/B) — Tools/blender/build_jeju_house.bat → jeju_house_kit.py. 로그: jeju_house_log.txt
        [MenuItem("Coast Run/Build Jeju Houses 3D (Blender)")]
        public static void BuildJejuHouses() => RunBat("build_jeju_house.bat", "jeju_house_log.txt");

        private static void RunBat(string bat, string log)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "blender"));
            string path = Path.Combine(dir, bat);
            if (!File.Exists(path)) { UnityEngine.Debug.LogError("[BlenderKit] 없음: " + path); return; }
            var psi = new ProcessStartInfo("cmd.exe", "/c \"" + path + "\"")
            {
                WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) { UnityEngine.Debug.LogError("[BlenderKit] 실행 실패"); return; }
                if (!p.WaitForExit(240000)) { UnityEngine.Debug.LogError("[BlenderKit] 240초 초과 — Blender 가 안 끝남"); return; }
                string tail = "";
                try { var lines = File.ReadAllLines(Path.Combine(dir, log)); tail = string.Join("\n", lines, Mathf.Max(0, lines.Length - 12), Mathf.Min(12, lines.Length)); } catch { }
                UnityEngine.Debug.LogWarning("[BlenderKit] exit=" + p.ExitCode + "\n" + tail);
            }
            AssetDatabase.Refresh();
        }
    }
}
