#nullable enable

/// <summary>Result of building a loot panel snapshot.</summary>
public sealed class ContainerLootPanelResult
{
	public ContainerLootErrorCode ErrorCode { get; init; }

	public ContainerLootPanelDto? Panel { get; init; }

	public static ContainerLootPanelResult Ok(ContainerLootPanelDto panel) =>
		new() { ErrorCode = ContainerLootErrorCode.None, Panel = panel };

	public static ContainerLootPanelResult Fail(ContainerLootErrorCode code) =>
		new() { ErrorCode = code };
}
