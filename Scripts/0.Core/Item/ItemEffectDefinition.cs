public abstract class ItemEffectDefinition
{
}

public abstract class UseItemEffectDefinition : ItemEffectDefinition
{
}

public abstract class EquippedItemEffectDefinition : ItemEffectDefinition
{
}

public sealed class RestoreHealthEffectDefinition : UseItemEffectDefinition
{
    public DiceExpression HealDice { get; set; } = new();
    public int FlatHealAmount { get; set; }
}

public sealed class DamageReductionEffectDefinition : EquippedItemEffectDefinition
{
    /// <summary>
    /// If set, reduction applies only to this exact damage type (takes precedence over <see cref="DamageFamily"/>).
    /// </summary>
    public DamageTypeDefinition? DamageType { get; set; }

    /// <summary>
    /// Applies to all damage types in this family when <see cref="DamageType"/> is not set.
    /// If both are null, reduction applies to all incoming damage.
    /// </summary>
    public DamageFamily? DamageFamily { get; set; }
    public int ReductionAmount { get; set; }
}