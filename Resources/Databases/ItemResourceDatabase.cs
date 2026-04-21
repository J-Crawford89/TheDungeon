using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemResourceDatabase : Resource
{
	[Export] public Array<ItemResource> Items { get; set; } = [];
}
