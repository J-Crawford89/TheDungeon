using Xunit;

public sealed class PlayerDefenseAggregationHelperTests
{
	[Fact]
	public void RecomputeFromEquippedArmor_AggregatesArmorPenaltyAndDamageReduction()
	{
		var slash = new DamageTypeDefinition("slash", "Slashing", DamageFamily.Physical);
		var fire = new DamageTypeDefinition("fire", "Fire", DamageFamily.Elemental);
		var armor = new ArmorDefinition
		{
			Id = "chain",
			ArmorBonus = 3,
			AgilityPenalty = 2,
			Effects =
			[
				new DamageReductionEffectDefinition { DamageType = slash, ReductionAmount = 2 },
				new DamageReductionEffectDefinition { DamageFamily = DamageFamily.Elemental, ReductionAmount = 1 },
				new DamageReductionEffectDefinition { DamageType = slash, ReductionAmount = 1 },
				new DamageReductionEffectDefinition { DamageFamily = DamageFamily.Physical, ReductionAmount = 4 },
				new DamageReductionEffectDefinition { ReductionAmount = -2 },
			]
		};

		var player = new PlayerState();
		var instance = new ItemInstance { Definition = armor, Quantity = 1 };
		player.InventoryState.Items.Add(instance);
		// Same armor instance occupies two slots; should only be counted once.
		player.InventoryState.EquippedBySlot[EquipmentSlot.Head] = instance;
		player.InventoryState.EquippedBySlot[EquipmentSlot.Torso] = instance;

		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(player);

		Assert.Equal(3, player.TotalArmorBonus);
		Assert.Equal(2, player.TotalAgilityPenalty);
		Assert.Equal(6, player.TotalDamageReduction);
		Assert.Equal(-2, player.DamageReductionAllDamage);
		Assert.Equal(3, player.DamageReductionByDamageTypeId["slash"]);
		Assert.Equal(4, player.DamageReductionByDamageFamily[DamageFamily.Physical]);
		Assert.Equal(1, player.DamageReductionByDamageFamily[DamageFamily.Elemental]);
		Assert.Equal(5, PlayerDamageReductionHelper.TotalDamageReductionFor(player, slash));
		Assert.Equal(-1, PlayerDamageReductionHelper.TotalDamageReductionFor(player, fire));
	}
}

