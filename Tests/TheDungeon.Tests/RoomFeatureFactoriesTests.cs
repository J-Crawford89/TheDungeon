using Xunit;

public sealed class RoomFeatureFactoriesTests
{
	[Fact]
	public void Trap_WhenDiscoverDcPositive_StartsHidden()
	{
		var trap = RoomFeatureFactories.CreateTrapFeature(new TrapDefinition
		{
			Id = "t",
			Name = "Trap",
			DiscoverDc = 12,
			DisarmDc = 10,
		});

		Assert.False(trap.Traps[0].IsRevealed);
	}

	[Fact]
	public void Trap_WhenDiscoverDcZero_StartsRevealed()
	{
		var trap = RoomFeatureFactories.CreateTrapFeature(new TrapDefinition
		{
			Id = "t",
			Name = "Trap",
			DiscoverDc = 0,
			DisarmDc = 10,
		});

		Assert.True(trap.Traps[0].IsRevealed);
	}

	[Fact]
	public void Treasure_WhenDiscoverDcPositive_StartsHidden()
	{
		var treasure = RoomFeatureFactories.CreateTreasureFeature(new TreasureDefinition
		{
			Id = "gold",
			Name = "Coins",
			DiscoverDc = 10,
			GrantKind = TreasureKind.Currency,
			CurrencyGrant = new CoinPurse { Copper = 5 },
		});

		Assert.False(treasure.TreasureItems[0].IsRevealed);
	}

	[Fact]
	public void Npc_WhenDiscoverDcPositive_StartsHidden()
	{
		var npc = RoomFeatureFactories.CreateNpcFeature(new NpcDefinition
		{
			Id = "n",
			Name = "Hermit",
			DiscoverDc = 11,
		});

		Assert.False(npc.NPCs[0].IsRevealed);
	}

	[Fact]
	public void Lore_WhenDiscoverDcPositive_StartsHidden()
	{
		var lore = RoomFeatureFactories.CreateLoreFeature(new LoreDefinition
		{
			Id = "l",
			Name = "Scrawl",
			DiscoverDc = 9,
		});

		Assert.False(lore.Lore[0].IsRevealed);
	}
}
