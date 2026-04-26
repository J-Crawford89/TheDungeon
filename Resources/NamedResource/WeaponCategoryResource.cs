using Godot;

[GlobalClass]
public partial class WeaponCategoryResource : ProficiencyTargetResource
{
    public override ProficiencyTargetType TargetType => ProficiencyTargetType.WeaponCategory;
}