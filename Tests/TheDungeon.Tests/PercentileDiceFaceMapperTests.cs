using Xunit;

namespace TheDungeon.Tests;

public sealed class PercentileDiceFaceMapperTests
{
	[Theory]
	[InlineData(6, 0, 6)]
	[InlineData(24, 20, 4)]
	[InlineData(30, 30, 10)]
	[InlineData(100, 0, 10)]
	public void Map_MapsTensAndOnes(int total, int expectedTens, int expectedOnes)
	{
		var faces = PercentileDiceFaceMapper.Map(total);
		Assert.Equal(expectedTens, faces.PercentileTensFace);
		Assert.Equal(expectedOnes, faces.OnesDieFace);
	}
}
