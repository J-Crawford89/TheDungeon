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
}
