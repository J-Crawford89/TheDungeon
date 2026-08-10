using System;
using System.Collections.Generic;
using System.Numerics;

public readonly record struct DicePlayAreaWallSpec(string Name, Vector3 Position, Vector3 Size);

public static class DicePlayAreaWallLayout
{
	public static IReadOnlyList<DicePlayAreaWallSpec> Build(Vector3 halfExtents, float wallHeight, float wallThickness)
	{
		if (halfExtents.X <= 0f)
			throw new ArgumentOutOfRangeException(nameof(halfExtents), "X half-extent must be positive.");
		if (halfExtents.Z <= 0f)
			throw new ArgumentOutOfRangeException(nameof(halfExtents), "Z half-extent must be positive.");
		if (wallHeight <= 0f)
			throw new ArgumentOutOfRangeException(nameof(wallHeight));
		if (wallThickness <= 0f)
			throw new ArgumentOutOfRangeException(nameof(wallThickness));

		var hx = halfExtents.X;
		var hz = halfExtents.Z;
		var h = wallHeight;
		var t = wallThickness;
		var yCenter = h * 0.5f;

		return
		[
			new("WallPosX", new Vector3(hx + t * 0.5f, yCenter, 0f), new Vector3(t, h, hz * 2f + t * 2f)),
			new("WallNegX", new Vector3(-hx - t * 0.5f, yCenter, 0f), new Vector3(t, h, hz * 2f + t * 2f)),
			new("WallPosZ", new Vector3(0f, yCenter, hz + t * 0.5f), new Vector3(hx * 2f + t * 2f, h, t)),
			new("WallNegZ", new Vector3(0f, yCenter, -hz - t * 0.5f), new Vector3(hx * 2f + t * 2f, h, t)),
		];
	}
}
