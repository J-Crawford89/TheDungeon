using Godot;

[GlobalClass]
public partial class WeaponGroupResource : ProficiencyTargetResource
{
    public override ProficiencyTargetType TargetType => ProficiencyTargetType.WeaponGroup;
}