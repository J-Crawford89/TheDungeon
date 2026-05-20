using System;
using System.Numerics;

/// <summary>Builds a toss impulse from a random horizontal direction and magnitude ranges.</summary>
public static class DieTossImpulseBuilder
{
	public static Vector3 Build(float horizontalAngleRadians, float horizontalMagnitude, float upwardMagnitude)
	{
		var horizontal = new Vector3(MathF.Cos(horizontalAngleRadians), 0f, MathF.Sin(horizontalAngleRadians));
		return horizontal * horizontalMagnitude + Vector3.UnitY * upwardMagnitude;
	}
}
