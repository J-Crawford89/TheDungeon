/// <summary>Discriminator for in-room <c>ContainerFeature</c> subtypes (narrative / UI labels).</summary>
public enum ContainerLootKind
{
	Salvage,
	Corpse,
	Chest,
	/// <summary>Another or future <c>ContainerFeature</c> subclass.</summary>
	Other,
}
