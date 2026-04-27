using Godot;

[GlobalClass]
public partial class  AbilityResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
}