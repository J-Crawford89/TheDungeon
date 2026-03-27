public static class TreasureMapper
{
	public static TreasureDefinition ToDomain(TreasureResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			GrantKind = resource.GrantKind,
			ValueInGp = resource.ValueInGp,
			InventoryItemId = resource.InventoryItemId
		};
}
