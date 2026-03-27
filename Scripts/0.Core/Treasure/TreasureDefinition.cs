public sealed class TreasureDefinition
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public TreasureKind GrantKind { get; set; }
	public int ValueInGp { get; set; }
	public string InventoryItemId { get; set; } = string.Empty;
}