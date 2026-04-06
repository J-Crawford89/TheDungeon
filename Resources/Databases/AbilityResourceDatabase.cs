using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AbilityResourceDatabase : Resource
{
	[Export] public Array<AbilityResource> Abilities { get; set; } = [];
}
