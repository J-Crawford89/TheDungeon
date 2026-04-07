public sealed class WeaponDefinition : EquipmentDefinition
{
    public List<DamageComponent> DamageComponents { get; set; } = new();
}