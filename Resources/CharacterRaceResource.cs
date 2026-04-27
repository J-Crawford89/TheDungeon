using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterRaceResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public int BaseHp { get; set; }
    [Export] public Array<LevelAbilityGrantResource> AbilityGrants { get; set; } = [];
    [Export] public Array<ItemResource> StartingEquipment { get; set; } = [];
    [Export] public Array<ProficiencyGrantResource> ProficiencyGrants { get; set; } = [];
}