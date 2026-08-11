using System;
using System.Collections.Generic;
using System.Numerics;

public readonly record struct DieTossKinematicsResult(
	Vector3 AngularVelocity,
	float EffectiveRollingRadius,
	float NominalRollingAngularSpeed,
	float InitialSurfaceSlipSpeed);

/// <summary>
/// Couples a die's release rotation to its translational throw. The main angular velocity is the
/// no-slip rolling rate for the actual convex hull, scaled by a controlled release coupling.
/// </summary>
public static class DieTossKinematicsBuilder
{
	public static DieTossKinematicsResult Build(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion startOrientation,
		Vector3 linearVelocity,
		float rollCoupling,
		Vector3 tumbleAxis,
		float tumbleSpeed)
	{
		if (hullPoints == null || hullPoints.Count < 4)
			throw new ArgumentException("A die requires at least four hull points.", nameof(hullPoints));
		if (rollCoupling < 0f)
			throw new ArgumentOutOfRangeException(nameof(rollCoupling));
		if (tumbleSpeed < 0f)
			throw new ArgumentOutOfRangeException(nameof(tumbleSpeed));

		var horizontalVelocity = new Vector3(linearVelocity.X, 0f, linearVelocity.Z);
		var horizontalSpeed = horizontalVelocity.Length();
		if (horizontalSpeed < 0.0001f)
			return new DieTossKinematicsResult(Vector3.Zero, 0f, 0f, 0f);

		var travelDirection = horizontalVelocity / horizontalSpeed;
		var worldRollAxis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, travelDirection));
		var radius = ComputeEffectiveRollingRadius(
			hullPoints,
			startOrientation,
			Vector3.UnitY);
		var nominalAngularSpeed = horizontalSpeed / radius;
		var angularVelocity = worldRollAxis * (nominalAngularSpeed * rollCoupling);

		if (tumbleSpeed > 0f)
		{
			if (tumbleAxis.LengthSquared() < 0.0001f)
				throw new ArgumentException("Tumble axis cannot be zero when tumble speed is positive.", nameof(tumbleAxis));
			angularVelocity += Vector3.Normalize(tumbleAxis) * tumbleSpeed;
		}

		return new DieTossKinematicsResult(
			angularVelocity,
			radius,
			nominalAngularSpeed,
			ComputeLowestContactSlipSpeed(
				hullPoints,
				startOrientation,
				linearVelocity,
				angularVelocity,
				Vector3.UnitY));
	}

	public static float ComputeEffectiveRollingRadius(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion startOrientation,
		Vector3 worldSurfaceNormal)
	{
		if (hullPoints == null || hullPoints.Count < 4)
			throw new ArgumentException("A die requires at least four hull points.", nameof(hullPoints));
		if (worldSurfaceNormal.LengthSquared() < 0.0001f)
			throw new ArgumentException("Surface normal cannot be zero.", nameof(worldSurfaceNormal));

		var orientation = Quaternion.Normalize(startOrientation);
		var surfaceNormal = Vector3.Normalize(worldSurfaceNormal);
		var center = Vector3.Zero;
		for (var i = 0; i < hullPoints.Count; i++)
			center += hullPoints[i];
		center /= hullPoints.Count;

		var radius = 0f;
		for (var i = 0; i < hullPoints.Count; i++)
		{
			var worldOffset = Vector3.Transform(hullPoints[i] - center, orientation);
			radius = MathF.Max(radius, -Vector3.Dot(worldOffset, surfaceNormal));
		}

		if (radius < 0.05f)
			throw new ArgumentException("Hull does not have a usable rolling radius.", nameof(hullPoints));
		return radius;
	}
	public static float ComputeOriginHeightForFloorClearance(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion startOrientation,
		float floorHeight,
		float clearance)
	{
		if (hullPoints == null || hullPoints.Count < 4)
			throw new ArgumentException("A die requires at least four hull points.", nameof(hullPoints));
		if (clearance < 0f)
			throw new ArgumentOutOfRangeException(nameof(clearance));

		var orientation = Quaternion.Normalize(startOrientation);
		var lowestOffset = float.PositiveInfinity;
		for (var i = 0; i < hullPoints.Count; i++)
			lowestOffset = MathF.Min(
				lowestOffset,
				Vector3.Transform(hullPoints[i], orientation).Y);
		return floorHeight + clearance - lowestOffset;
	}
	private static float ComputeLowestContactSlipSpeed(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion startOrientation,
		Vector3 linearVelocity,
		Vector3 angularVelocity,
		Vector3 worldSurfaceNormal)
	{
		var orientation = Quaternion.Normalize(startOrientation);
		var surfaceNormal = Vector3.Normalize(worldSurfaceNormal);
		var center = Vector3.Zero;
		for (var i = 0; i < hullPoints.Count; i++)
			center += hullPoints[i];
		center /= hullPoints.Count;

		var lowestProjection = float.PositiveInfinity;
		var worldOffsets = new Vector3[hullPoints.Count];
		for (var i = 0; i < hullPoints.Count; i++)
		{
			worldOffsets[i] = Vector3.Transform(hullPoints[i] - center, orientation);
			lowestProjection = MathF.Min(
				lowestProjection,
				Vector3.Dot(worldOffsets[i], surfaceNormal));
		}

		var bestSlip = float.PositiveInfinity;
		for (var i = 0; i < worldOffsets.Length; i++)
		{
			if (Vector3.Dot(worldOffsets[i], surfaceNormal) > lowestProjection + 0.001f)
				continue;
			var pointVelocity = linearVelocity + Vector3.Cross(angularVelocity, worldOffsets[i]);
			var tangentVelocity = pointVelocity -
				surfaceNormal * Vector3.Dot(pointVelocity, surfaceNormal);
			bestSlip = MathF.Min(bestSlip, tangentVelocity.Length());
		}

		return float.IsFinite(bestSlip) ? bestSlip : 0f;
	}
}
