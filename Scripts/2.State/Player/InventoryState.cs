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
}