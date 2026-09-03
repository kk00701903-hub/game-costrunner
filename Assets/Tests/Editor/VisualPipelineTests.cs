using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

[TestFixture]
public class VisualPipelineTests
{
    [Test]
    public void CharacterAnimParams_HashesAreStable()
    {
        Assert.AreEqual(Animator.StringToHash("Speed"), CharacterAnimParams.Speed);
        Assert.AreEqual(Animator.StringToHash("Grounded"), CharacterAnimParams.Grounded);
        Assert.AreEqual(Animator.StringToHash("Slide"), CharacterAnimParams.Slide);
        Assert.AreEqual(Animator.StringToHash("Jump"), CharacterAnimParams.Jump);
        Assert.AreEqual(Animator.StringToHash("Dead"), CharacterAnimParams.Dead);
    }

    [Test]
    public void MaterialLibrary_ResourcePath()
    {
        Assert.AreEqual("347/MaterialLibrary", MaterialLibrary.ResourcePath);
    }

    [Test]
    public void URP_IsAssignedInGraphicsSettings()
    {
        Assert.IsNotNull(GraphicsSettings.defaultRenderPipeline,
            "URP must be assigned — run Tools > 347 > Setup Visual Pipeline");
    }

    [Test]
    public void MaterialLibraryAsset_ExistsUnderResources()
    {
        Assert.IsTrue(
            File.Exists("Assets/Resources/347/MaterialLibrary.asset") ||
            Resources.Load<MaterialLibrary>("347/MaterialLibrary") != null,
            "MaterialLibrary missing under Resources/347");
    }

    // The three tests below assert assets belonging to the retired A-0347 project
    // (Assets/Scripts + Assets/_Project). Those assets are gone from the repo, so the
    // tests fail and keep the whole suite red. Ignored rather than deleted so the
    // dead-tree teardown stays a single reviewable change.
    // Delete this file together with Assets/Scripts and Assets/_Project.

    [Test]
    [Ignore("A-0347 legacy: Assets/_Guide/FreeAssetMap.json no longer exists. Remove with the dead-tree teardown.")]
    public void FreeAssetMap_DocumentsKenneyMapping()
    {
        Assert.IsTrue(File.Exists("Assets/_Guide/FreeAssetMap.json"));
    }

    [Test]
    [Ignore("A-0347 legacy: Resources/Character/Doha is empty. CoastRun uses _CoastRun/Art/Character/GirlSkater.")]
    public void DohaModel_OrCharacterMedium_Present()
    {
        bool hasPrefab = Resources.Load<GameObject>("Character/Doha/DohaModel") != null;
        bool hasFbx = Resources.Load<GameObject>("Character/Doha/characterMedium") != null;
        Assert.IsTrue(hasPrefab || hasFbx, "3D character asset missing under Resources/Character/Doha");
    }

    [Test]
    [Ignore("A-0347 legacy: Resources/Tracks is empty. CoastRun streams segments procedurally.")]
    public void ZoneTrackTokens_MatchAssetRequestNames()
    {
        // File names used by ZoneDirector.AllowsTile must stay stable.
        string[] expected = { "Track_Arcade", "Track_Overpass", "Track_Flooded", "Track_Depot", "Track_CornerL", "Track_CornerR" };
        foreach (string name in expected)
        {
            string fbx = "Assets/Resources/Tracks/" + name + ".fbx";
            string glb = "Assets/Resources/Tracks/" + name + ".glb";
            Assert.IsTrue(File.Exists(fbx) || File.Exists(glb) || name.StartsWith("Track_Corner"),
                "Missing zone track: " + name + " (TestCatalog corners cover Corner*)");
        }
    }
}
