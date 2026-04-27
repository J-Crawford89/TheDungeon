#nullable enable

/// <summary>Result of loot-all or loot-selected commands.</summary>
public sealed class ContainerLootTransferResult
{
	public ContainerLootErrorCode ErrorCode { get; init; }

	public int StacksGranted { get; init; }

	public int StacksSkippedMissingDefinition { get; init; }

	public bool RemovedContainerFromRoom { get; init; }

	public static ContainerLootTransferResult Fail(ContainerLootErrorCode code) =>
		new() { ErrorCode = code };

	public static ContainerLootTransferResult Ok(
		int stacksGranted,
		int stacksSkippedMissingDefinition,
		bool removedContainerFromRoom) =>
		new()
		{
			ErrorCode = ContainerLootErrorCode.None,
			StacksGranted = stacksGranted,
			StacksSkippedMissingDefinition = stacksSkippedMissingDefinition,
			RemovedContainerFromRoom = removedContainerFromRoom,
		};
}
