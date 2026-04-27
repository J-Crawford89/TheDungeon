using System.Linq;
using Xunit;

public sealed class RoomExitsTests
{
	[Fact]
	public void SetAndGet_RoundTripsPerDirection()
	{
		var exits = new RoomExits();
		exits.Set(HorizontalDirection.North, RoomConnectionType.Passage);
		exits.Set(HorizontalDirection.East, RoomConnectionType.Door);

		Assert.Equal(RoomConnectionType.Passage, exits.Get(HorizontalDirection.North));
		Assert.Equal(RoomConnectionType.Door, exits.Get(HorizontalDirection.East));
		Assert.Equal(RoomConnectionType.None, exits.Get(HorizontalDirection.South));
	}

	[Fact]
	public void All_ReturnsFourCardinalEntries()
	{
		var exits = new RoomExits();
		var all = exits.All().ToList();

		Assert.Equal(4, all.Count);
		Assert.Equal(HorizontalDirection.North, all[0].Side);
		Assert.Equal(HorizontalDirection.West, all[3].Side);
	}
}
