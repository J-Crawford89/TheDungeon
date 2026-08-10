using Godot;

public static class DieFaceRotationResolver
{
	public static Godot.Quaternion Resolve(
		DieFaceCalibration calibration,
		int faceValue,
		Godot.Quaternion currentBodyRotation,
		Godot.Vector3 targetDirectionWorld)
	{
		if (!calibration.TryGetFaceOrientation(faceValue, out var faceOrientation))
			return currentBodyRotation;

		var solved = DieFaceOrientationSolver.SolveNearestFromFaceOrientation(
			ToNumeric(faceOrientation),
			ToNumeric(currentBodyRotation),
			ToNumeric(targetDirectionWorld.Normalized()));
		return ToGodot(solved);
	}

	private static System.Numerics.Vector3 ToNumeric(Godot.Vector3 v) => new(v.X, v.Y, v.Z);

	private static System.Numerics.Quaternion ToNumeric(Godot.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

	private static Godot.Quaternion ToGodot(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
}
