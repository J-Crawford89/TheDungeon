using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LoreResourceDatabase : Resource
{
	[Export] public Array<LoreResource> LoreEntries { get; set; } = [];
}
