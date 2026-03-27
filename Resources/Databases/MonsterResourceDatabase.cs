using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MonsterResourceDatabase : Resource
{
	[Export] public Array<MonsterResource> Monsters { get; set; } = [];
}
