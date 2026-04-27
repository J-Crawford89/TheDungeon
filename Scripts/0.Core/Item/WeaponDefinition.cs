public sealed class WeaponDefinition : EquipmentDefinition
{
    public List<AttackDefinition> Attacks { get; set; } = new();
    public WeaponCategoryDefinition Category { get; set; } = new();
    public WeaponSubCategoryDefinition SubCategory { get; set; } = new();
    public WeaponGroupDefinition Group { get; set; } = new();
    public WeaponSubGroupDefinition SubGroup { get; set; } = new();

	/// <summary>
	/// While the player owns this weapon, grants <see cref="ProficiencyTargetType.Weapon"/> proficiency at this rank.
	/// <see cref="ProficiencyRank.Untrained"/> means no ownership grant.
	/// </summary>
	public ProficiencyRank OwnershipProficiencyRank { get; set; } = ProficiencyRank.Untrained;
}