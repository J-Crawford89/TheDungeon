using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TrapResourceDatabase : Resource
{
	[Export] public Array<TrapResource> Traps { get; set; } = [];
}
