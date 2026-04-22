public sealed class TreasureDefinition
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public TreasureKind GrantKind { get; set; }
	public int ValueInGp { get; set; }
	public string InventoryItemId { get; set; } = string.Empty;

	/// <summary>Inspect perception DC; 0 or less means immediately visible.</summary>
	public int DiscoverDc { get; set; }
}