using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CharacterBackgroundResourceDatabase : Resource
{
	[Export] public Array<CharacterBackgroundResource> CharacterBackgrounds { get; set; } = [];
}
