using Xunit;

public sealed class InventoryEquipmentOperationsTests
{
	private static ItemInstance Stack(string id, int qty) =>
		new()
		{
			Definition = new ItemDefinition { Id = id, Name = id, MaxStackSize = 99 },
			Quantity = qty,
		};

	private static EquipmentDefinition EquipDef(string id, params EquipmentSlot[] slots) =>
		new()
		{
			Id = id,
			Name = id,
			MaxStackSize = 1,
			Slots = [..slots],
		};

	[Fact]
	public void TryEquipOneFromBackpackRow_EquipsSingleStackRow()
	{
		var helm = new EquipmentDefinition
		{
			Id = "helm",
			Name = "Helm",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var row = new ItemInstance { Definition = helm, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.Single(inv.Items);
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var e) && ReferenceEquals(e, row));
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_StackSplit_CreatesNewInstance()
	{
		var helm = new EquipmentDefinition
		{
			Id = "helm",
			Name = "Helm",
			MaxStackSize = 99,
			Slots = [EquipmentSlot.Head],
		};
		var row = new ItemInstance { Definition = helm, Quantity = 3 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.Equal(2, row.Quantity);
		Assert.Equal(2, inv.Items.Count);
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var equipped) && equipped != null);
		Assert.NotSame(row, equipped);
		Assert.Equal(1, equipped!.Quantity);
		Assert.Equal(helm.Id, equipped.Definition.Id);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_Swap_ReplacesEquipped()
	{
		var oldHelm = new EquipmentDefinition
		{
			Id = "old",
			Name = "Old",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var newHelm = new EquipmentDefinition
		{
			Id = "new",
			Name = "New",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var worn = new ItemInstance { Definition = oldHelm, Quantity = 1 };
		var backpack = new ItemInstance { Definition = newHelm, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { worn, backpack },
			EquippedBySlot = { [EquipmentSlot.Head] = worn },
		};

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, backpack));
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var e) && ReferenceEquals(e, backpack));
		Assert.Contains(worn, inv.Items);
		Assert.Contains(backpack, inv.Items);
	}

	[Fact]
	public void TryUnequipSlot_ClearsSlot()
	{
		var helm = new EquipmentDefinition { Id = "h", Name = "H", Slots = [EquipmentSlot.Head] };
		var row = new ItemInstance { Definition = helm, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { row },
			EquippedBySlot = { [EquipmentSlot.Head] = row },
		};

		Assert.True(InventoryEquipmentOperations.TryUnequipSlot(inv, EquipmentSlot.Head));
		Assert.Null(inv.EquippedBySlot[EquipmentSlot.Head]);
		Assert.Contains(row, inv.Items);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_NonEquipment_ReturnsFalse()
	{
		var inv = new InventoryState { Items = { Stack("junk", 1) } };
		Assert.False(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, inv.Items[0]));
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_MultipleDistinctSlots_ReturnsFalse()
	{
		var def = new EquipmentDefinition
		{
			Id = "flex",
			Name = "Flex",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponMainHand2],
		};
		var row = new ItemInstance { Definition = def, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.False(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRowToSlot(inv, row, EquipmentSlot.WeaponMainHand2));
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.WeaponMainHand2, out var e) && ReferenceEquals(e, row));
	}

	[Fact]
	public void TryEquipOneFromBackpackRowToSlot_DisallowedSlot_ReturnsFalse()
	{
		var def = new EquipmentDefinition
		{
			Id = "flex",
			Name = "Flex",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponMainHand1],
		};
		var row = new ItemInstance { Definition = def, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.False(InventoryEquipmentOperations.TryEquipOneFromBackpackRowToSlot(inv, row, EquipmentSlot.Head));
	}

	[Fact]
	public void FootprintSlots_DedupesOccupiedSlots()
	{
		var def = new EquipmentDefinition
		{
			OccupiedSlots = [EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponOffHand1, EquipmentSlot.WeaponMainHand1],
		};
		var fp = InventoryEquipmentOperations.FootprintSlots(def);
		Assert.Equal(2, fp.Count);
		Assert.Equal(EquipmentSlot.WeaponMainHand1, fp[0]);
		Assert.Equal(EquipmentSlot.WeaponOffHand1, fp[1]);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_TwoHanded_AssignsSameInstanceToBothSlots_UnequipClearsBoth()
	{
		var pierce = new DamageTypeDefinition("p", "Piercing", DamageFamily.Physical);
		var gs = new WeaponDefinition
		{
			Id = "gs",
			Name = "Greatsword",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponMainHand1],
			OccupiedSlots = [EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponOffHand1],
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d6, InD20CheckPool = false },
					0,
					pierce),
			],
		};
		var row = new ItemInstance { Definition = gs, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.WeaponMainHand1, out var a) && a != null);
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.WeaponOffHand1, out var b) && b != null);
		Assert.Same(a, b);

		Assert.True(InventoryEquipmentOperations.TryUnequipSlot(inv, EquipmentSlot.WeaponOffHand1));
		Assert.False(inv.EquippedBySlot.TryGetValue(EquipmentSlot.WeaponMainHand1, out var x) && x != null);
		Assert.False(inv.EquippedBySlot.TryGetValue(EquipmentSlot.WeaponOffHand1, out var y) && y != null);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_TwoHanded_EvictsDistinctItemsInFootprint()
	{
		var pierce = new DamageTypeDefinition("p", "Piercing", DamageFamily.Physical);
		var shield = new EquipmentDefinition
		{
			Id = "sh",
			Name = "Shield",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponOffHand1],
		};
		var gs = new WeaponDefinition
		{
			Id = "gs",
			Name = "Greatsword",
			MaxStackSize = 1,
			OccupiedSlots = [EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponOffHand1],
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d6, InD20CheckPool = false },
					0,
					pierce),
			],
		};
		var shieldInst = new ItemInstance { Definition = shield, Quantity = 1 };
		var gsRow = new ItemInstance { Definition = gs, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { shieldInst, gsRow },
			EquippedBySlot = { [EquipmentSlot.WeaponOffHand1] = shieldInst },
		};

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, gsRow));
		Assert.Same(gsRow, inv.EquippedBySlot[EquipmentSlot.WeaponMainHand1]);
		Assert.Same(gsRow, inv.EquippedBySlot[EquipmentSlot.WeaponOffHand1]);
		Assert.Contains(shieldInst, inv.Items);
	}

	[Fact]
	public void TryAutoEquipStartingGearOne_UsesFirstEmptyAllowedSlot()
	{
		var flex = EquipDef("flex", EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponMainHand2);
		var row = new ItemInstance { Definition = flex, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryAutoEquipStartingGearOne(inv, row));
		Assert.Same(row, inv.EquippedBySlot[EquipmentSlot.WeaponMainHand1]);
	}

	[Fact]
	public void TryAutoEquipStartingGearOne_MovesOccupantWhenOccupantCanSafelyRelocate()
	{
		var flexible = EquipDef("flex", EquipmentSlot.WeaponMainHand1, EquipmentSlot.WeaponMainHand2);
		var locked = EquipDef("lock", EquipmentSlot.WeaponMainHand1);
		var occupied = new ItemInstance { Definition = flexible, Quantity = 1 };
		var incoming = new ItemInstance { Definition = locked, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { occupied, incoming },
			EquippedBySlot =
			{
				[EquipmentSlot.WeaponMainHand1] = occupied,
				[EquipmentSlot.WeaponMainHand2] = null
			}
		};

		Assert.True(InventoryEquipmentOperations.TryAutoEquipStartingGearOne(inv, incoming));
		Assert.Same(incoming, inv.EquippedBySlot[EquipmentSlot.WeaponMainHand1]);
		Assert.Same(occupied, inv.EquippedBySlot[EquipmentSlot.WeaponMainHand2]);
	}

	[Fact]
	public void TryAutoEquipStartingGearOne_LeavesIncomingInBackpackWhenOccupantCannotRelocate()
	{
		var occupiedDef = EquipDef("occupied", EquipmentSlot.Head);
		var incomingDef = EquipDef("incoming", EquipmentSlot.Head);
		var occupied = new ItemInstance { Definition = occupiedDef, Quantity = 1 };
		var incoming = new ItemInstance { Definition = incomingDef, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { occupied, incoming },
			EquippedBySlot = { [EquipmentSlot.Head] = occupied }
		};

		Assert.False(InventoryEquipmentOperations.TryAutoEquipStartingGearOne(inv, incoming));
		Assert.Same(occupied, inv.EquippedBySlot[EquipmentSlot.Head]);
		Assert.Contains(incoming, inv.Items);
	}
}
