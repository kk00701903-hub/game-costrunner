using NUnit.Framework;

[TestFixture]
public class PickupTests
{
    [TestCase("Item_Coin", PickupKind.Coin)]
    [TestCase("Item_Tag", PickupKind.Tag)]
    [TestCase("Item_BoosterCell", PickupKind.BoosterCell)]
    [TestCase("Item_Battery", PickupKind.BoosterCell)]
    [TestCase("Item_Shield", PickupKind.Shield)]
    [TestCase("Item_Scan", PickupKind.ReverseScan)]
    [TestCase("Item_ReverseScan", PickupKind.ReverseScan)]
    [TestCase("Item_Tape", PickupKind.DeckTape)]
    [TestCase("Item_DeckTape", PickupKind.DeckTape)]
    [TestCase("Item_DeckPiece", PickupKind.DeckPiece)]
    [TestCase("Item_Letter", PickupKind.Letter)]
    [TestCase("SupplyCrate_Med", PickupKind.DeckTape)]
    [TestCase("SupplyCrate_Food", PickupKind.Coin)]
    [TestCase("Crate", PickupKind.BoosterCell)]
    public void KindFromName_MapsKnownAssets(string name, PickupKind expected)
    {
        Assert.AreEqual(expected, Pickup.KindFromName(name));
    }

    [Test]
    public void KindFromName_NullOrEmpty_DefaultsToCoin()
    {
        Assert.AreEqual(PickupKind.Coin, Pickup.KindFromName(null));
        Assert.AreEqual(PickupKind.Coin, Pickup.KindFromName(""));
    }

    [Test]
    public void IsActiveItem_OnlyBoostersAndTools()
    {
        Assert.IsTrue(Pickup.IsActiveItem(PickupKind.BoosterCell));
        Assert.IsTrue(Pickup.IsActiveItem(PickupKind.Shield));
        Assert.IsTrue(Pickup.IsActiveItem(PickupKind.ReverseScan));
        Assert.IsFalse(Pickup.IsActiveItem(PickupKind.Coin));
        Assert.IsFalse(Pickup.IsActiveItem(PickupKind.Letter));
        Assert.IsFalse(Pickup.IsActiveItem(PickupKind.DeckTape));
    }
}
