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

	[Fact]
	public void MainView_ForContainer_Salvage_UsesOpenedKeyWhenWasOpened()
	{
		var closed = new SalvageFeature();
		Assert.Equal(PresentationIconKeys.MainView.ContainerSalvage(), PresentationIconKeys.MainView.ForContainer(closed));
		closed.WasOpened = true;
		Assert.Equal(PresentationIconKeys.MainView.ContainerSalvageOpened(), PresentationIconKeys.MainView.ForContainer(closed));
	}

	[Fact]
	public void MainView_ForContainer_Corpse_UsesOpenedSegmentWhenWasOpened()
	{
		var corpse = new CorpseFeature { SourceMonsterDefinitionId = "rat" };
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/container/corpse/rat",
			PresentationIconKeys.MainView.ForContainer(corpse));
		corpse.WasOpened = true;
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/container/corpse_opened/rat",
			PresentationIconKeys.MainView.ForContainer(corpse));
	}

	[Fact]
	public void MainView_ForContainer_Chest_Locked_IgnoresWasOpened()
	{
		var chest = new ChestFeature { Locked = true, WasOpened = true };
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/container/chest_locked",
			PresentationIconKeys.MainView.ForContainer(chest));
	}

	[Fact]
	public void MainView_ForContainer_Chest_Unlocked_OpenedVsClosed()
	{
		var chest = new ChestFeature { Locked = false, WasOpened = false };
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/container/chest",
			PresentationIconKeys.MainView.ForContainer(chest));
		chest.WasOpened = true;
		Assert.Equal($"{PresentationIconKeys.MainView.Root}/container/chest_opened",
			PresentationIconKeys.MainView.ForContainer(chest));
	}
}
