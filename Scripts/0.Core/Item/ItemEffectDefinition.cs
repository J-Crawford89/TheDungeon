public abstract class ItemEffectDefinition
{
}

public sealed class RestoreHealthEffectDefinition : ItemEffectDefinition
{
    public DiceExpression HealDice { get; set; } = new();
    public int FlatHealAmount { get; set; }
}