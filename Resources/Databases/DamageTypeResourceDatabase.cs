using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DamageTypeResourceDatabase : Resource
{
	[Export] public Array<DamageTypeResource> DamageTypes { get; set; } = [];
}
