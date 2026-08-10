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
			worldRollAxis);
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
			horizontalSpeed * MathF.Abs(1f - rollCoupling));
	}

	public static float ComputeEffectiveRollingRadius(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion startOrientation,
		Vector3 worldRollAxis)
	{
		if (hullPoints == null || hullPoints.Count < 4)
			throw new ArgumentException("A die requires at least four hull points.", nameof(hullPoints));
		if (worldRollAxis.LengthSquared() < 0.0001f)
			throw new ArgumentException("Roll axis cannot be zero.", nameof(worldRollAxis));

		var orientation = Quaternion.Normalize(startOrientation);
		var localAxis = Vector3.Normalize(Vector3.Transform(
			Vector3.Normalize(worldRollAxis),
			Quaternion.Inverse(orientation)));
		var center = Vector3.Zero;
		for (var i = 0; i < hullPoints.Count; i++)
			center += hullPoints[i];
		center /= hullPoints.Count;

		var radiusSquared = 0f;
		for (var i = 0; i < hullPoints.Count; i++)
		{
			var fromCenter = hullPoints[i] - center;
			var perpendicular = fromCenter - localAxis * Vector3.Dot(fromCenter, localAxis);
			radiusSquared = MathF.Max(radiusSquared, perpendicular.LengthSquared());
		}

		if (radiusSquared < 0.0025f)
			throw new ArgumentException("Hull does not have a usable rolling radius.", nameof(hullPoints));
		return MathF.Sqrt(radiusSquared);
	}
}
