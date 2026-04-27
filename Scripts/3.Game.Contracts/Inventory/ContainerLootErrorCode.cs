/// <summary>Outcome for container loot panel queries and loot commands.</summary>
public enum ContainerLootErrorCode
{
	None = 0,
	NoCurrentFloor,
	NoCurrentRoom,
	ContainerOrdinalOutOfRange,
	InvalidRowSelection,
	EmptyRowSelection,
}
