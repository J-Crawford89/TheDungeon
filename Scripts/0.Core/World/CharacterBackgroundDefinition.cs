public sealed class CharacterBackgroundDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StartingGold { get; set; }
    public List<LevelAbilityGrant> AbilityGrants { get; set; } = new();
    public List<string> StartingEquipment { get; set; } = new();
    public List<string> Proficiencies { get; set; } = new();
}

