public abstract class EquipmentDefinition : ItemDefinition
{
    public List<EquipmentSlot> Slots { get; set; } = new();
}