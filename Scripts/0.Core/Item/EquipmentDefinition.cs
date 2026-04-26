public class EquipmentDefinition : ItemDefinition
{
	/// <summary>
	/// When <see cref="OccupiedSlots"/> is empty: valid single-slot targets (player picks if more than one distinct value).
	/// When <see cref="OccupiedSlots"/> is non-empty: ignored for placement; use <see cref="OccupiedSlots"/> as the footprint.
	/// </summary>
	public List<EquipmentSlot> Slots { get; set; } = new();

	/// <summary>
	/// When non-empty, the item occupies every listed slot at once (same <see cref="ItemInstance"/> reference in each).
	/// When empty, the item uses single-slot rules via <see cref="Slots"/>.
	/// </summary>
	public List<EquipmentSlot> OccupiedSlots { get; set; } = new();

    public MaterialDefinition Material { get; set; } = new();
}