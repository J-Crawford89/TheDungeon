#nullable enable
using Godot;

[GlobalClass]
public partial class TreasureResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public TreasureKind GrantKind { get; set; }
	[Export] public int ValueInGp { get; set; }
	[Export] public string InventoryItemId { get; set; } = string.Empty;
	[Export] public int DiscoverDc { get; set; } = 0;

	[Export] public Texture2D? Icon { get; set; }
}
