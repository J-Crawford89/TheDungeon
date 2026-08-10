using System;
using System.Collections.Generic;
using System.Numerics;

public static class DiceSpawnPositionPicker
{
	public static Vector3 Pick(
		float unitX,
		float unitZ,
		Vector3 boundsHalfExtents,
		float height,
		float edgeMargin)
	{
		if (boundsHalfExtents.X < 0f || boundsHalfExtents.Z < 0f)
			throw new ArgumentOutOfRangeException(nameof(boundsHalfExtents));
		if (edgeMargin < 0f)
			throw new ArgumentOutOfRangeException(nameof(edgeMargin));

		unitX = Math.Clamp(unitX, 0f, 1f);
		unitZ = Math.Clamp(unitZ, 0f, 1f);
		var usableX = MathF.Max(0f, boundsHalfExtents.X - edgeMargin);
		var usableZ = MathF.Max(0f, boundsHalfExtents.Z - edgeMargin);
		return new Vector3(
			(unitX * 2f - 1f) * usableX,
			height,
			(unitZ * 2f - 1f) * usableZ);
	}

	public static bool HasMinimumSeparation(
		Vector3 candidate,
		IReadOnlyList<Vector3> existing,
		float minimumDistance)
	{
		if (minimumDistance < 0f)
			throw new ArgumentOutOfRangeException(nameof(minimumDistance));

		var requiredSquared = minimumDistance * minimumDistance;
		foreach (var point in existing)
		{
			var delta = new Vector2(candidate.X - point.X, candidate.Z - point.Z);
			if (delta.LengthSquared() < requiredSquared)
				return false;
		}
		return true;
	}
}
