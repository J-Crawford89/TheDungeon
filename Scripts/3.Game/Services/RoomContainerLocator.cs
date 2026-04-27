#nullable enable

/// <summary>Resolves <see cref="ContainerFeature"/> instances by enumeration order in <see cref="DungeonRoom.Features"/>.</summary>
public static class RoomContainerLocator
{
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
