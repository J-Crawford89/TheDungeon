using Xunit;

public sealed class PlayerAttackOptionsResolverTests
{
	[Fact]
	public void ResolveWeaponChoiceDescriptors_NoWeapons_OnlyUnarmed()
	{
		var inv = new InventoryState();
		var list = PlayerAttackOptionsResolver.ResolveWeaponChoiceDescriptors(inv);
		Assert.Single(list);
		Assert.Equal("Unarmed", list[0].Label);
		Assert.Equal(TargetPayloadKind.PlayerAttackWeaponPick, list[0].Payload.Kind);
		Assert.True(list[0].Payload.AttackChoice.IsUnarmed);
	}

	[Fact]
	public void ResolveWeaponChoiceDescriptors_MainHandWeapon_AddsOption()
	{
		var pierce = new DamageTypeDefinition("p", "Piercing", DamageFamily.Physical);
		var club = new WeaponDefinition
		{
			Id = "club",
			Name = "Club",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponMainHand1],
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d6, InD20CheckPool = false },
					0,
					pierce),
			],
		};
		var inst = new ItemInstance { Definition = club, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { inst },
			EquippedBySlot = { [EquipmentSlot.WeaponMainHand1] = inst },
		};

		var list = PlayerAttackOptionsResolver.ResolveWeaponChoiceDescriptors(inv);
		Assert.Equal(2, list.Count);
		Assert.Equal("Unarmed", list[0].Label);
		Assert.Contains("Club", list[1].Label);
		Assert.False(list[1].Payload.AttackChoice.IsUnarmed);
		Assert.Equal(EquipmentSlot.WeaponMainHand1, list[1].Payload.AttackChoice.WeaponSlotIfAny);
	}

	[Fact]
	public void HasWeaponChoiceBeyondUnarmed_FalseWhenNoWeapons()
	{
		Assert.False(PlayerAttackOptionsResolver.HasWeaponChoiceBeyondUnarmed(new InventoryState()));
	}
}
