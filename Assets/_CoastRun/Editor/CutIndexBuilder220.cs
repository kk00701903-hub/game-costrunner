using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 220차: 컷씬 그림 목록(컷 id → Resources 경로)을 Resources/CoastRun/컷씬이미지/_index.txt 로 만든다.
    ///   게임은 이 목록으로 필요한 그림 한 장만 읽는다(예전: 첫 컷씬에서 494장 전부 로드 → 멈춤·메모리 321 MB).
    ///   빌드 직전에 자동으로 다시 만든다. 수동: 메뉴 Coast Run/Store/220 - Rebuild cutscene image index
    public class CutIndexBuilder220 : IPreprocessBuildWithReport
    {
        const string Root = "Assets/Resources/CoastRun/컷씬이미지";
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) { Build(); }

        [MenuItem("Coast Run/Store/220 - Rebuild cutscene image index")]
        public static void Build()
        {
            if (!Directory.Exists(Root)) return;
            string pathsFile = Path.Combine(Root, "_paths.txt");
            var folders = new List<string>();
            if (File.Exists(pathsFile)) foreach (var l in File.ReadAllLines(pathsFile, Encoding.UTF8)) { var t = l.Trim().TrimStart('﻿'); if (t.Length > 0) folders.Add(t); }
            else folders.Add("CoastRun/컷씬이미지");
            var seen = new Dictionary<string, string>();
            var sb = new StringBuilder();
            foreach (var f in folders)
            {
                string dir = "Assets/Resources/" + f;
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir);
                System.Array.Sort(files, System.StringComparer.Ordinal);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".jpg" && ext != ".png" && ext != ".jpeg") continue;
                    string name = Path.GetFileNameWithoutExtension(file);
                    int i = name.LastIndexOf("Cut_", System.StringComparison.Ordinal); if (i < 0) continue;
                    string id = name.Substring(i);
                    if (seen.ContainsKey(id)) continue;
                    seen[id] = f + "/" + name;
                    sb.Append(id).Append('\t').Append(f).Append('/').Append(name).Append('\n');
                }
            }
            string outPath = Path.Combine(Root, "_index.txt");
            string text = sb.ToString();
            if (!File.Exists(outPath) || File.ReadAllText(outPath, Encoding.UTF8) != text)
            {
                File.WriteAllText(outPath, text, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(outPath);
            }
            Debug.Log($"[CutIndex220] 컷 그림 {seen.Count}장 목록 → {outPath}");
        }
    }
}
