public abstract class NamedDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class MaterialDefinition : NamedDefinition { }
public sealed class WeaponCategoryDefinition : NamedDefinition { }
public sealed class WeaponSubCategoryDefinition : NamedDefinition { }
public sealed class WeaponGroupDefinition : NamedDefinition { }
public sealed class WeaponSubGroupDefinition : NamedDefinition { }