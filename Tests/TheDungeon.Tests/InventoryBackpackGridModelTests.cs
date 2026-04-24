using System;
using System.Linq;
using Xunit;

public sealed class InventoryBackpackGridModelTests
{
	[Fact]
	public void BuildUnequippedRows_EmptyInventory_EmptyNoOverflow()
	{
		var state = new InventoryState();
		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 16);
		Assert.False(overflow);
		Assert.Empty(rows);
	}

	[Fact]
	public void BuildUnequippedRows_ExcludesEquippedInstanceIds()
	{
		var equipped = new ItemInstance
		{
			InstanceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
			Definition = new ItemDefinition { Id = "sword" },
			Quantity = 1,
		};
		var inBag = new ItemInstance
		{
			InstanceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
			Definition = new ItemDefinition { Id = "potion" },
			Quantity = 2,
		};
		var state = new InventoryState
		{
			Items = [equipped, inBag],
			EquippedBySlot = new Dictionary<EquipmentSlot, ItemInstance?>
			{
				[EquipmentSlot.WeaponMainHand1] = equipped,
			},
		};

		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 16);
		Assert.False(overflow);
		Assert.Single(rows);
		Assert.Same(inBag, rows[0]);
	}

	[Fact]
	public void BuildUnequippedRows_PreservesItemsOrder()
	{
		var a = new ItemInstance { Definition = new ItemDefinition { Id = "a" }, Quantity = 1 };
		var b = new ItemInstance { Definition = new ItemDefinition { Id = "b" }, Quantity = 1 };
		var c = new ItemInstance { Definition = new ItemDefinition { Id = "c" }, Quantity = 1 };
		var state = new InventoryState { Items = [a, b, c] };

		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 16);
		Assert.False(overflow);
		Assert.Equal(new[] { "a", "b", "c" }, rows.Select(r => r.Definition.Id).ToArray());
	}

	[Fact]
	public void BuildUnequippedRows_SixteenNoOverflow()
	{
		var state = new InventoryState();
		for (var i = 0; i < 16; i++)
		{
			state.Items.Add(new ItemInstance
			{
				Definition = new ItemDefinition { Id = $"item{i}" },
				Quantity = 1,
			});
		}

		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 16);
		Assert.False(overflow);
		Assert.Equal(16, rows.Count);
	}

	[Fact]
	public void BuildUnequippedRows_SeventeenOverflow_TruncatesToMax()
	{
		var state = new InventoryState();
		for (var i = 0; i < 17; i++)
		{
			state.Items.Add(new ItemInstance
			{
				Definition = new ItemDefinition { Id = $"item{i}" },
				Quantity = 1,
			});
		}

		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 16);
		Assert.True(overflow);
		Assert.Equal(16, rows.Count);
		Assert.Equal("item0", rows[0].Definition.Id);
		Assert.Equal("item15", rows[15].Definition.Id);
	}

	[Fact]
	public void BuildUnequippedRows_MaxSlotsZero_EmptyAndOverflowWhenItems()
	{
		var state = new InventoryState
		{
			Items = [new ItemInstance { Definition = new ItemDefinition { Id = "x" }, Quantity = 1 }],
		};
		var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(state, 0);
		Assert.True(overflow);
		Assert.Empty(rows);
	}
}
