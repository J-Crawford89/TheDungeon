using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NpcResourceDatabase : Resource
{
	[Export] public Array<NpcResource> Npcs { get; set; } = [];
}
