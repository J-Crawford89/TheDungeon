using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelAbilityGrantResourceDatabase : Resource
{
    [Export] public Array<LevelAbilityGrantResource> AbilityGrants { get; set; } = [];
}
