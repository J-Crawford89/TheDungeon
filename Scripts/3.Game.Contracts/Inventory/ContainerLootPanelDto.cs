#nullable enable
using System.Collections.Generic;

/// <summary>Read-only snapshot for a loot picker UI; rebuild via <c>TryBuildPanel</c> each time.</summary>
public sealed class ContainerLootPanelDto
{
	public required ContainerLootKind Kind { get; init; }

	/// <summary>Narrative label derived from kind (e.g. &quot;salvage pile&quot;).</summary>
	public required string ContainerKindLabel { get; init; }

	/// <summary>Panel heading: Chest, Corpse, Salvage, …</summary>
	public required string PanelTitle { get; init; }

	/// <summary>Source context (trap name, monster name); empty for chest.</summary>
	public required string PanelSubtitle { get; init; }

	public required IReadOnlyList<ContainerLootStackRowDto> Rows { get; init; }

	/// <summary>Best-effort hint when taking all stacks would crowd the backpack grid.</summary>
	public bool LikelyCrowdedAfterTakeAll { get; init; }
}
