using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterRaceResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public int BaseHp { get; set; }
    [Export] public Array<string> Abilities { get; set; } = new();
    [Export] public Array<string> StartingEquipment { get; set; } = new();
    [Export] public Array<string> Proficiencies { get; set; } = new();
}