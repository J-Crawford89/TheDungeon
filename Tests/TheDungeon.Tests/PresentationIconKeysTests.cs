using Xunit;

public sealed class PresentationIconKeysTests
{
	[Fact]
	public void Inventory_Item_UsesInventoryRoot()
	{
		Assert.Equal($"{PresentationIconKeys.Inventory.Root}/item/health_potion",
			PresentationIconKeys.Inventory.Item("health_potion"));
	}

	[Fact]
	public void MainView_Monster_UsesMainViewRoot()
	{
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/monster/rat",
			PresentationIconKeys.MainView.Monster("rat"));
	}
}
