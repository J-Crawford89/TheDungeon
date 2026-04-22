using System.Collections.Generic;
using System.Linq;

public sealed class InventoryState
{
	public List<ItemInstance> Items { get; set; } = new();

	/// <summary>Sums quantities for stacks whose <see cref="ItemInstance.Definition"/> id matches.</summary>
	public int SumQuantityForDefinitionId(string itemDefinitionId)
	{
		if (string.IsNullOrWhiteSpace(itemDefinitionId))
			return 0;

		var id = itemDefinitionId.Trim();
		var sum = 0;
		foreach (var row in Items)
		{
			if (row.Definition.Id == id)
				sum += row.Quantity;
		}

		return sum;
	}

	/// <summary>Decrements quantity by one for the first stack matching <paramref name="itemDefinitionId"/>.</summary>
	/// <returns><see langword="true"/> if an item was consumed.</returns>
	public bool TryConsumeOne(string itemDefinitionId)
	{
		if (string.IsNullOrWhiteSpace(itemDefinitionId))
			return false;

		var id = itemDefinitionId.Trim();
		for (var i = 0; i < Items.Count; i++)
		{
			var row = Items[i];
			if (row.Definition.Id != id || row.Quantity <= 0)
				continue;

			row.Quantity--;
			if (row.Quantity <= 0)
				Items.RemoveAt(i);

			return true;
		}

		return false;
	}

	/// <summary>Increment an existing stack for <paramref name="definition"/>, or add a new row of quantity 1.</summary>
	public void AddOrStackOne(ItemDefinition definition)
	{
		var existing = Items.FirstOrDefault(i => i.Definition.Id == definition.Id);
		if (existing != null && existing.Quantity < definition.MaxStackSize)
		{
			existing.Quantity++;
			return;
		}

		Items.Add(new ItemInstance
		{
			Definition = definition,
			Quantity = 1,
		});
	}
}