public static class TreasureMapper
{
	public static TreasureDefinition ToDomain(TreasureResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			GrantKind = resource.GrantKind,
			CurrencyGrant = CoinPurseMapper.ToDomain(resource.CurrencyGrant),
			InventoryItemId = resource.InventoryItemId,
			DiscoverDc = resource.DiscoverDc,
		};
}
