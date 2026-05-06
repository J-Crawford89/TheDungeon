#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;

	[Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;

	[Export] public int ValueInCopper { get; set; }
	[Export] public int MaxStackSize { get; set; } = 1;

	[Export] public bool CanDrop { get; set; } = true;
	[Export] public bool CanSell { get; set; } = true;

	[Export] public Texture2D? Icon { get; set; }

	/// <summary>Inventory / notebook UI; when null, <see cref="Icon"/> is used.</summary>
	[Export] public Texture2D? InventoryIcon { get; set; }

	[Export] public Array<ItemEffectResource> Effects { get; set; } = [];
}
