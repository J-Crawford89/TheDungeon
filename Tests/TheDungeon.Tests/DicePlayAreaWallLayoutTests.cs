using System;
using System.Linq;
using System.Numerics;
using Xunit;

namespace TheDungeon.Tests;

public sealed class DicePlayAreaWallLayoutTests
{
	[Fact]
	public void Build_CreatesAllFourWallsWithOverlappingCorners()
	{
		var walls = DicePlayAreaWallLayout.Build(new Vector3(8.5f, 0f, 4.5f), 12f, 0.25f);

		Assert.Equal(4, walls.Count);
		Assert.Equal(new[] { "WallNegX", "WallNegZ", "WallPosX", "WallPosZ" }, walls.Select(w => w.Name).Order().ToArray());

		var top = Assert.Single(walls, w => w.Name == "WallNegZ");
		Assert.Equal(new Vector3(0f, 6f, -4.625f), top.Position);
		Assert.Equal(new Vector3(17.5f, 12f, 0.25f), top.Size);

		var right = Assert.Single(walls, w => w.Name == "WallPosX");
		Assert.Equal(new Vector3(8.625f, 6f, 0f), right.Position);
		Assert.Equal(new Vector3(0.25f, 12f, 9.5f), right.Size);
	}

	[Theory]
	[InlineData(0f, 4f, 0.25f)]
	[InlineData(8f, 0f, 0.25f)]
	[InlineData(8f, 4f, 0f)]
	public void Build_RejectsNonPositiveDimensions(float halfExtentX, float height, float thickness)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			DicePlayAreaWallLayout.Build(new Vector3(halfExtentX, 0f, 4f), height, thickness));
	}
}
