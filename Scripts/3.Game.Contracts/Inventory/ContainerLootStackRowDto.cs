#nullable enable

/// <summary>One stack in a container <c>Contents</c> list, for the loot panel (each stack maps to a <c>LootableItemControl</c>).</summary>
public sealed class ContainerLootStackRowDto
{
	/// <summary>Index into the live <see cref="ContainerFeature.Contents"/> list (same as the control&apos;s stack index).</summary>
	public required int RowIndex { get; init; }

	public required string ItemDefinitionId { get; init; }

	public required string DisplayName { get; init; }

	public required int Quantity { get; init; }

	/// <summary>Set when this stack requires harvest checks; both null if none.</summary>
	public int? HarvestDc { get; init; }

	public AbilityScore? HarvestAbility { get; init; }
}
