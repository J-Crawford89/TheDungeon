using Godot;

/// <summary>
/// Proficiency grant target for <see cref="ProficiencyTargetType.UnarmedStrikes"/>.
/// Use <see cref="NamedResource.Id"/> <c>all</c> for blanket unarmed proficiency, or match an <see cref="AttackDefinition.Id"/> for one mode.
/// Assign as <see cref="MonsterResource.ProficiencyGrants"/> entries or class/race grants in the editor (create .tres targets there; do not edit serialized files by hand unless you know the format).
/// </summary>
[GlobalClass]
public partial class UnarmedStrikesProficiencyTargetResource : ProficiencyTargetResource
{
	public override ProficiencyTargetType TargetType => ProficiencyTargetType.UnarmedStrikes;
}
