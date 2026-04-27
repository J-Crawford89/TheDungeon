#nullable enable

/// <summary>Resolves <see cref="ContainerFeature"/> instances by enumeration order in <see cref="DungeonRoom.Features"/>.</summary>
public static class RoomContainerLocator
{
	public static bool HasLootableStacks(ContainerFeature container)
	{
		foreach (var row in container.Contents)
		{
			if (string.IsNullOrWhiteSpace(row.ItemDefinitionId) || row.Quantity <= 0)
				continue;
			return true;
		}

		return false;
	}

	public static bool CurrentRoomHasLootableContainers(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;
		foreach (var f in room.Features)
		{
			if (f is ContainerFeature cf && HasLootableStacks(cf))
				return true;
		}

		return false;
	}

	public static bool TryGetNthContainer(DungeonRoom room, int ordinal, out ContainerFeature? container)
	{
		container = null;
		var n = 0;
		foreach (var feature in room.Features)
		{
			if (feature is not ContainerFeature cf)
				continue;
			if (n == ordinal)
			{
				container = cf;
				return true;
			}

			n++;
		}

		return false;
	}
}
