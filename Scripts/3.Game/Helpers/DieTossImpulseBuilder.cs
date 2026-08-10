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

	/// <summary>
	/// Converts a desired launch velocity into an impulse. Using mass here keeps differently
	/// weighted visual bodies moving at comparable speeds instead of applying the same delta momentum.
	/// </summary>
	public static Vector3 BuildForTargetVelocity(
		float horizontalAngleRadians,
		float horizontalSpeed,
		float upwardSpeed,
		float bodyMass)
	{
		if (horizontalSpeed < 0f)
			throw new ArgumentOutOfRangeException(nameof(horizontalSpeed));
		if (upwardSpeed < 0f)
			throw new ArgumentOutOfRangeException(nameof(upwardSpeed));
		if (bodyMass <= 0f)
			throw new ArgumentOutOfRangeException(nameof(bodyMass));

		return Build(horizontalAngleRadians, horizontalSpeed, upwardSpeed) * bodyMass;
	}
}
