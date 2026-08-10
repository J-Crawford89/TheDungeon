using System;
using System.Numerics;

/// <summary>
/// Applies a calibrated face-up pose, aligns world up to a requested world direction,
/// and adds optional spin around that direction.
/// </summary>
public static class DieFaceOrientationSolver
{
	public static Quaternion SolveFromFaceOrientation(
		Quaternion faceOrientation,
		Vector3 targetDirectionWorld,
		float spinDegrees) =>
		Solve(faceOrientation, targetDirectionWorld, spinDegrees);

	/// <summary>
	/// Returns the valid face-up orientation whose spin around the target direction is closest
	/// to the body's current orientation. This avoids an unrelated yaw jump during a forced landing.
	/// </summary>
	public static Quaternion SolveNearestFromFaceOrientation(
		Quaternion faceOrientation,
		Quaternion currentBodyRotation,
		Vector3 targetDirectionWorld)
	{
		targetDirectionWorld = Vector3.Normalize(targetDirectionWorld);
		var baseOrientation = Quaternion.Normalize(
			RotationBetween(Vector3.UnitY, targetDirectionWorld) * faceOrientation);
		var relative = Quaternion.Normalize(
			currentBodyRotation * Quaternion.Inverse(baseOrientation));
		var closestSpin = ExtractTwist(relative, targetDirectionWorld);
		return Quaternion.Normalize(closestSpin * baseOrientation);
	}

	private static Quaternion Solve(
		Quaternion faceOrientation,
		Vector3 targetDirectionWorld,
		float spinDegrees)
	{
		targetDirectionWorld = Vector3.Normalize(targetDirectionWorld);
		var alignWorldUp = RotationBetween(Vector3.UnitY, targetDirectionWorld);
		var spin = Quaternion.CreateFromAxisAngle(targetDirectionWorld, spinDegrees * (MathF.PI / 180f));
		return Quaternion.Normalize(spin * alignWorldUp * faceOrientation);
	}

	private static Quaternion RotationBetween(Vector3 from, Vector3 to)
	{
		var dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);
		if (dot > 0.9999f)
			return Quaternion.Identity;
		if (dot < -0.9999f)
		{
			var axis = MathF.Abs(from.X) < 0.9f ? Vector3.UnitX : Vector3.UnitZ;
			axis = Vector3.Normalize(Vector3.Cross(from, axis));
			return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
		}

		var cross = Vector3.Cross(from, to);
		return Quaternion.Normalize(new Quaternion(cross.X, cross.Y, cross.Z, 1f + dot));
	}

	private static Quaternion ExtractTwist(Quaternion rotation, Vector3 axis)
	{
		var vector = new Vector3(rotation.X, rotation.Y, rotation.Z);
		var projected = axis * Vector3.Dot(vector, axis);
		var twist = new Quaternion(projected.X, projected.Y, projected.Z, rotation.W);
		var lengthSquared = twist.LengthSquared();
		if (lengthSquared < 0.000001f)
			return Quaternion.Identity;
		return Quaternion.Normalize(twist);
	}
}
