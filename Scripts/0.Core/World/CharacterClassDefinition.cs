public sealed class CharacterClassDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int BaseHp { get; set; }
    public List<AbilityDefinition> Abilities { get; set; } = new();
    public List<string> StartingEquipment { get; set; } = new();
    public List<string> Proficiencies { get; set; } = new();
}

