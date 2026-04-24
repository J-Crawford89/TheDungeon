#nullable enable
using Godot;

[GlobalClass]
public partial class MainViewTraversalIcons : Resource
{
	[Export] public Texture2D? StairsIcon { get; set; }
	[Export] public Texture2D? HoleIcon { get; set; }
	[Export] public Texture2D? LadderIcon { get; set; }
}
