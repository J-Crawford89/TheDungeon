using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TreasureResourceDatabase : Resource
{
	[Export] public Array<TreasureResource> Treasures { get; set; } = [];
}
