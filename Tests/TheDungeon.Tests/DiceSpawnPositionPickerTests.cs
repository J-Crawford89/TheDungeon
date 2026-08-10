using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace TheDungeon.Tests;

public sealed class DiceSpawnPositionPickerTests
{
	[Fact]
	public void Pick_MapsRandomUnitsInsideMarginAdjustedBounds()
	{
		var min = DiceSpawnPositionPicker.Pick(0f, 0f, new Vector3(7f, 0f, 3.5f), 1.35f, 0.75f);
		var max = DiceSpawnPositionPicker.Pick(1f, 1f, new Vector3(7f, 0f, 3.5f), 1.35f, 0.75f);

		Assert.Equal(new Vector3(-6.25f, 1.35f, -2.75f), min);
		Assert.Equal(new Vector3(6.25f, 1.35f, 2.75f), max);
	}

	[Fact]
	public void Pick_ClampsRandomUnits()
	{
		var point = DiceSpawnPositionPicker.Pick(-2f, 3f, new Vector3(2f, 0f, 1f), 1f, 0f);

		Assert.Equal(new Vector3(-2f, 1f, 1f), point);
	}

	[Fact]
	public void HasMinimumSeparation_ChecksOnlyPlaySurfaceDistance()
	{
		var existing = new List<Vector3> { new(0f, 20f, 0f) };

		Assert.False(DiceSpawnPositionPicker.HasMinimumSeparation(
			new Vector3(0.5f, 1f, 0f),
			existing,
			1f));
		Assert.True(DiceSpawnPositionPicker.HasMinimumSeparation(
			new Vector3(1.1f, 1f, 0f),
			existing,
			1f));
	}

	[Fact]
	public void Pick_RejectsNegativeMargin()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			DiceSpawnPositionPicker.Pick(0.5f, 0.5f, Vector3.One, 1f, -0.1f));
	}
}
