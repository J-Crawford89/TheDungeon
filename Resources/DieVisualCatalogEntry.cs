using Godot;

[GlobalClass]
public partial class DieVisualCatalogEntry : Resource
{
	[Export] public DieType DieType { get; set; }
	[Export] public DieVisualRole Role { get; set; } = DieVisualRole.Standard;
	[Export] public PackedScene VisualScene { get; set; } = null!;
}
