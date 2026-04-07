public sealed class PotionDefinition : ConsumableDefinition
{
    public List<ItemEffectDefinition> Effects { get; set; } = new();
}