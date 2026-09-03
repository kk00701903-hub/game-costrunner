using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Turns Art FBX + PNG into Resources/CoastRun assets PrefabLibrary can load.
    public static class CoastArtImportMenu
    {
        private const string ArtRoot = "Assets/_CoastRun/Art";
        /// Prefer project-wide Resources so Resources.Load("CoastRun/…") always hits.
        private const string ResourcesRoot = "Assets/Resources/CoastRun";
        private const string AltResourcesRoot = "Assets/_CoastRun/Resources/CoastRun";

        private static readonly (string fbxRel, string prefabName)[] FbxMap =
        {
            ("Character/GirlSkater.fbx", "GirlSkater"),
            ("Props/Pole_WireSet.fbx", "Pole_WireSet"),
            ("Props/Obstacle_Cone.fbx", "Obstacle_Cone"),
            ("Tiles/Tile_Promenade_30m.fbx", "Tile_Promenade_30m"),
            ("Tiles/Tile_TownL_ShopA.fbx", "Tile_TownL_ShopA"),
            ("Tiles/Tile_SeaWallR_30m.fbx", "Tile_SeaWallR_30m"),
            // Seasonal / reusable props (Blender batch)
            ("Props/Seasonal/Prop_Bench.fbx", "Prop_Bench"),
            ("Props/Seasonal/Prop_StreetLamp.fbx", "Prop_StreetLamp"),
            ("Props/Seasonal/Prop_CherryTree.fbx", "Prop_CherryTree"),
            ("Props/Seasonal/Prop_Maple.fbx", "Prop_Maple"),
            ("Props/Seasonal/Prop_Pine.fbx", "Prop_Pine"),
            ("Props/Seasonal/Prop_Palm.fbx", "Prop_Palm"),
            ("Props/Seasonal/Prop_Snowman.fbx", "Prop_Snowman"),
            ("Props/Seasonal/Prop_Buoy.fbx", "Prop_Buoy"),
            ("Props/Seasonal/Prop_CafeUmbrella.fbx", "Prop_CafeUmbrella"),
            ("Props/Seasonal/Prop_IceCreamCart.fbx", "Prop_IceCreamCart"),
            ("Props/Seasonal/Prop_Pumpkin.fbx", "Prop_Pumpkin"),
            ("Props/Seasonal/Prop_LeafPile.fbx", "Prop_LeafPile"),
            ("Props/Seasonal/Prop_SnowBank.fbx", "Prop_SnowBank"),
            ("Props/Seasonal/Prop_VendingMachine.fbx", "Prop_VendingMachine"),
            ("Props/Seasonal/Prop_Planter.fbx", "Prop_Planter"),
            ("Props/Seasonal/Prop_FestivalLantern.fbx", "Prop_FestivalLantern"),
            ("Props/Seasonal/Obstacle_Barrier.fbx", "Obstacle_Barrier"),
            ("Props/Seasonal/Obstacle_ConeTall.fbx", "Obstacle_ConeTall"),
            ("Props/Seasonal/Obstacle_Crate.fbx", "Obstacle_Crate"),
            ("Props/Seasonal/Obstacle_WetFloorSign.fbx", "Obstacle_WetFloorSign"),
            ("Props/Seasonal/Obstacle_BikeFallen.fbx", "Obstacle_BikeFallen"),
        };

        private static readonly (string srcRel, string dstName)[] PngMap =
        {
            ("Sky/SummerSky_Portrait.png", "SummerSky_Portrait.png"),
            ("Environment/Sea_Turquoise_Tile.png", "Sea_Turquoise_Tile.png"),
            ("Textures/Road_Promenade.png", "Road_Promenade.png"),
            ("UI/Icon_Coin.png", "Icon_Coin.png"),
            ("UI/Icon_Speed.png", "Icon_Speed.png"),
            ("UI/Icon_Magnet.png", "Icon_Magnet.png"),
            ("UI/Icon_Tower.png", "Icon_Tower.png"),
            ("UI/Icon_Him.png", "Icon_Him.png"),
            ("UI/Watch_Frame.png", "Watch_Frame.png"),
            ("UI/UI_Panel_Memory.png", "UI_Panel_Memory.png"),
            ("UI/UI_TitleBackground.png", "UI_TitleBackground.png"),
            ("UI/UI_CharacterHero.png", "UI_CharacterHero.png"),
        };

        [MenuItem("Tools/Coast Run/Auto Import Art → Resources")]
        public static void AutoImportArtToResources()
        {
            EnsureFolder(ResourcesRoot);
            AssetDatabase.StartAssetEditing();
            int prefabs = 0;
            int pngs = 0;
            try
            {
                foreach (var (srcRel, dstName) in PngMap)
                {
                    if (CopyOrRefreshPng(ArtRoot + "/" + srcRel, ResourcesRoot + "/" + dstName))
                        pngs++;
                }

                foreach (var (fbxRel, prefabName) in FbxMap)
                {
                    if (CreatePrefabFromFbx(ArtRoot + "/" + fbxRel, ResourcesRoot + "/" + prefabName + ".prefab"))
                        prefabs++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Mirror into _CoastRun/Resources for docs that mention that path.
            EnsureFolder(AltResourcesRoot);
            foreach (var (_, prefabName) in FbxMap)
            {
                string src = ResourcesRoot + "/" + prefabName + ".prefab";
                string dst = AltResourcesRoot + "/" + prefabName + ".prefab";
                if (File.Exists(ToDisk(src)))
                    AssetDatabase.CopyAsset(src, dst);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Coast Run Auto Import: {prefabs} prefabs, {pngs} pngs → {ResourcesRoot}");
            EnsureTransmissionTowerPrefab();
            ListMissing();
        }

        /// Saves a reusable transmission tower prefab for DestinationGate visuals.
        public static void EnsureTransmissionTowerPrefab()
        {
            EnsureFolder(ResourcesRoot);
            string prefabAsset = ResourcesRoot + "/TransmissionTower.prefab";
            if (File.Exists(ToDisk(prefabAsset)))
                return;

            var root = new GameObject("TransmissionTower");
            DestinationGate.CreateVisual(root.transform, 100f);
            foreach (var col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            PrefabUtility.SaveAsPrefabAsset(root, prefabAsset);
            Object.DestroyImmediate(root);
            Debug.Log("Created " + prefabAsset);
        }

        /// Batchmode entry: Unity -batchmode -quit -executeMethod CoastRun.Editor.CoastArtImportMenu.AutoImportArtToResources
        public static void AutoImportArtToResourcesBatch()
        {
            AutoImportArtToResources();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Coast Run/Refresh Art Import Notes")]
        public static void PrintImportGuide()
        {
            Debug.Log(
                "Coast Run MCP art:\n" +
                "Menu: Tools > Coast Run > Auto Import Art → Resources\n" +
                "Loads: " + ResourcesRoot + "\n" +
                "PrefabLibrary → Resources.Load(\"CoastRun/<name>\")");
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Coast Run/List Missing Prefab Resources")]
        public static void ListMissing()
        {
            foreach (var (_, prefabName) in FbxMap)
            {
                string path = ResourcesRoot + "/" + prefabName + ".prefab";
                bool hasPrefab = File.Exists(ToDisk(path));
                var loaded = Resources.Load<GameObject>("CoastRun/" + prefabName);
                Debug.Log(prefabName + "  file=" + hasPrefab + "  Resources.Load=" + (loaded != null));
            }
        }

        private static bool CopyOrRefreshPng(string srcAsset, string dstAsset)
        {
            string srcDisk = ToDisk(srcAsset);
            if (!File.Exists(srcDisk))
            {
                Debug.LogWarning("Missing PNG: " + srcAsset);
                return false;
            }

            string dstDisk = ToDisk(dstAsset);
            Directory.CreateDirectory(Path.GetDirectoryName(dstDisk) ?? "");
            if (File.Exists(dstDisk))
            {
                // Keep newer Art copy.
                File.Copy(srcDisk, dstDisk, true);
                return true;
            }

            return AssetDatabase.CopyAsset(srcAsset, dstAsset);
        }

        private static bool CreatePrefabFromFbx(string fbxAsset, string prefabAsset)
        {
            if (!File.Exists(ToDisk(fbxAsset)))
            {
                Debug.LogWarning("Missing FBX: " + fbxAsset);
                return false;
            }

            // Force import so ModelImporter finishes before we instantiate.
            AssetDatabase.ImportAsset(fbxAsset, ImportAssetOptions.ForceUpdate);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAsset);
            if (model == null)
            {
                Debug.LogError("FBX loaded null: " + fbxAsset);
                return false;
            }

            var instance = Object.Instantiate(model);
            instance.name = Path.GetFileNameWithoutExtension(prefabAsset);

            // Ensure meshes have a default lit material if none assigned.
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.name.Contains("Default-Material"))
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                                 ?? Shader.Find("Standard");
                    if (shader != null)
                        renderer.sharedMaterial = new Material(shader);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ToDisk(prefabAsset)) ?? "");
            PrefabUtility.SaveAsPrefabAsset(instance, prefabAsset);
            Object.DestroyImmediate(instance);
            Debug.Log("Prefab created: " + prefabAsset);
            return true;
        }

        private static void EnsureFolder(string assetFolder)
        {
            string disk = ToDisk(assetFolder);
            if (Directory.Exists(disk))
                return;

            string[] parts = assetFolder.Split('/');
            string cur = parts[0]; // Assets
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static string ToDisk(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
