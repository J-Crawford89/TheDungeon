using Godot;

public static class DieFaceRotationResolver
{
	public static Godot.Quaternion Resolve(
		DieFaceCalibration calibration,
		int faceValue,
		Godot.Quaternion currentBodyRotation,
		Godot.Vector3 cameraGlobalPosition,
		Godot.Vector3 dieGlobalPosition,
		float spinDegrees)
	{
		if (!calibration.TryGetFaceOrientation(faceValue, out var faceOrientation))
			return currentBodyRotation;

		var target = (cameraGlobalPosition - dieGlobalPosition).Normalized();
		var solved = DieFaceOrientationSolver.SolveFromFaceOrientation(
			ToNumeric(faceOrientation),
			ToNumeric(currentBodyRotation),
			ToNumeric(target),
			spinDegrees);
		return ToGodot(solved);
	}

	private static System.Numerics.Vector3 ToNumeric(Godot.Vector3 v) => new(v.X, v.Y, v.Z);

	private static System.Numerics.Quaternion ToNumeric(Godot.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

	private static Godot.Quaternion ToGodot(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
}
