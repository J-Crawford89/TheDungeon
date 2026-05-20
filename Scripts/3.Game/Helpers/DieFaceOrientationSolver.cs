using System;
using System.Numerics;

/// <summary>
/// Aligns a die so the calibrated face points toward the camera, with optional spin around that axis.
/// <paramref name="faceUpInBodySpace"/> is the body-space direction that should point at the camera when using
/// <paramref name="calibrationOrientation"/> as the reference pose for this face.
/// </summary>
public static class DieFaceOrientationSolver
{
	public static Quaternion Solve(
		Quaternion calibrationOrientation,
		Vector3 faceUpInBodySpace,
		Quaternion currentBodyRotation,
		Vector3 cameraDirectionWorld,
		float spinDegrees)
	{
		faceUpInBodySpace = Vector3.Normalize(faceUpInBodySpace);
		cameraDirectionWorld = Vector3.Normalize(cameraDirectionWorld);

		var faceWorld = Vector3.Normalize(Vector3.Transform(faceUpInBodySpace, calibrationOrientation));
		var align = RotationBetween(faceWorld, cameraDirectionWorld);
		var baseRotation = Quaternion.Normalize(align * currentBodyRotation);
		var spin = Quaternion.CreateFromAxisAngle(cameraDirectionWorld, spinDegrees * (MathF.PI / 180f));
		return Quaternion.Normalize(spin * baseRotation);
	}

	/// <summary>When calibration stores full face-up pose, face up in body space is +Y.</summary>
	public static Quaternion SolveFromFaceOrientation(
		Quaternion faceOrientation,
		Quaternion currentBodyRotation,
		Vector3 cameraDirectionWorld,
		float spinDegrees) =>
		Solve(faceOrientation, Vector3.UnitY, currentBodyRotation, cameraDirectionWorld, spinDegrees);

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
}
