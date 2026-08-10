using System;
using System.Numerics;
using Xunit;

namespace TheDungeon.Tests;

public sealed class DieTossImpulseBuilderTests
{
	[Fact]
	public void Build_HorizontalComponentIsUnitDirectionTimesMagnitude()
	{
		var impulse = DieTossImpulseBuilder.Build(0f, 5f, 2f);

		Assert.Equal(5f, impulse.X, 3);
		Assert.Equal(2f, impulse.Y, 3);
		Assert.Equal(0f, impulse.Z, 3);
	}

	[Fact]
	public void BuildForTargetVelocity_ScalesImpulseByBodyMass()
	{
		var impulse = DieTossImpulseBuilder.BuildForTargetVelocity(0f, 5f, 2f, 0.25f);

		Assert.Equal(1.25f, impulse.X, 3);
		Assert.Equal(0.5f, impulse.Y, 3);
		Assert.Equal(0f, impulse.Z, 3);
	}

	[Theory]
	[InlineData(-1f, 2f, 1f)]
	[InlineData(1f, -2f, 1f)]
	[InlineData(1f, 2f, 0f)]
	public void BuildForTargetVelocity_RejectsInvalidInputs(
		float horizontalSpeed,
		float upwardSpeed,
		float mass)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			DieTossImpulseBuilder.BuildForTargetVelocity(
				0f,
				horizontalSpeed,
				upwardSpeed,
				mass));
	}
}
