using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterClassResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public int BaseHp { get; set; }
    [Export] public Array<LevelAbilityGrantResource> AbilityGrants { get; set; } = [];
    [Export] public Array<ItemResource> StartingEquipment { get; set; } = [];
    [Export] public Array<string> Proficiencies { get; set; } = new();
}

