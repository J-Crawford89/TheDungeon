using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterRaceResourceDatabase : Resource
{
    [Export] public Array<CharacterRaceResource> CharacterRaces { get; set; } = [];
}