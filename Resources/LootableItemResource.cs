#nullable enable
using Godot;

[GlobalClass]
public partial class LootableItemResource : Resource
{
	/// <summary>Drag an item resource from the project; domain mapping uses <see cref="ItemResource.Id"/>.</summary>
	[Export] public ItemResource? ItemResource { get; set; }

	[Export] public int Quantity { get; set; } = 1;
}
