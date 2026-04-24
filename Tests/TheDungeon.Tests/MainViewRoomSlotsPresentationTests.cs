using Xunit;

public sealed class MainViewRoomSlotsPresentationTests
{
	[Fact]
	public void Enumerate_Monster_UsesMonsterPresentationKey()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 1,
					Definition = new MonsterDefinition { Id = "rat", Name = "Rat", Defense = 1 },
				},
			],
		});

		var slots = MainViewRoomSlots.Enumerate(room);
		Assert.Single(slots);
		Assert.Equal(PresentationIconKeys.MainView.Monster("rat"), slots[0].PresentationIconKey);
	}

	[Fact]
	public void Enumerate_GoldTreasure_UsesTreasureIdKey()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "gold_pile",
						Name = "Coins",
						GrantKind = TreasureKind.Gold,
						ValueInGp = 5,
						InventoryItemId = string.Empty,
					},
				},
			],
		});

		var slots = MainViewRoomSlots.Enumerate(room);
		Assert.Single(slots);
		Assert.Equal(PresentationIconKeys.MainView.Treasure("gold_pile"), slots[0].PresentationIconKey);
	}

	[Fact]
	public void Enumerate_GoldTreasure_WithInventoryItemIdStillUsesTreasureKey()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "copper_coins",
						Name = "A few copper coins",
						GrantKind = TreasureKind.Gold,
						ValueInGp = 2,
						InventoryItemId = "copper_coins",
					},
				},
			],
		});

		var slots = MainViewRoomSlots.Enumerate(room);
		Assert.Single(slots);
		Assert.Equal(PresentationIconKeys.MainView.Treasure("copper_coins"), slots[0].PresentationIconKey);
	}

	[Fact]
	public void Enumerate_TreasureWithInventoryItem_UsesItemKey()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "potion_pile",
						Name = "Potion",
						GrantKind = TreasureKind.InventoryItem,
						InventoryItemId = "health_potion",
					},
				},
			],
		});

		var slots = MainViewRoomSlots.Enumerate(room);
		Assert.Single(slots);
		Assert.Equal(PresentationIconKeys.MainView.Item("health_potion"), slots[0].PresentationIconKey);
	}
}
