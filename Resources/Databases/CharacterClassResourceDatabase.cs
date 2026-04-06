using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterClassResourceDatabase : Resource
{
    [Export] public Array<CharacterClassResource> CharacterClasses { get; set; } = [];
}