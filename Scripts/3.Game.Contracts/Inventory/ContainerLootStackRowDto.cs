#nullable enable

/// <summary>One row in a container <c>Contents</c> list, for UI display.</summary>
public sealed class ContainerLootStackRowDto
{
	/// <summary>Index into the live <see cref="ContainerFeature.Contents"/> list.</summary>
	public required int RowIndex { get; init; }

	public required string ItemDefinitionId { get; init; }

	public required string DisplayName { get; init; }

	public required int Quantity { get; init; }
}
