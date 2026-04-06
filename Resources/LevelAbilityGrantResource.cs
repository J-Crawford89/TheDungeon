using Godot;

[GlobalClass]
public partial class LevelAbilityGrantResource : Resource
{
    [Export] public int Level { get; set; }
    [Export] public string AbilityId { get; set; } = "";
}

