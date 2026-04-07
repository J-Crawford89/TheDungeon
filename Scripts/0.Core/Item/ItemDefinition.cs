public class ItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    public int ValueInGold { get; set; }
    public int MaxStackSize { get; set; } = 1;

    public bool CanDrop { get; set; } = true;
    public bool CanSell { get; set; } = true;
}