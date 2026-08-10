using System;
using System.Numerics;
using Xunit;

namespace TheDungeon.Tests;

public sealed class DieFaceOrientationSolverTests
{
	[Fact]
	public void SolveFromFaceOrientation_SpinChangesResultButFaceStillPointsUp()
	{
		var calibration = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);

		var a = DieFaceOrientationSolver.SolveFromFaceOrientation(calibration, Vector3.UnitY, 0f);
		var b = DieFaceOrientationSolver.SolveFromFaceOrientation(calibration, Vector3.UnitY, 45f);

		Assert.NotEqual(a, b);
		AssertFacePointsUp(calibration, a);
		AssertFacePointsUp(calibration, b);
	}

	[Fact]
	public void SolveNearestFromFaceOrientation_PreservesAnAlreadyValidYaw()
	{
		var calibration = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2f);
		var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 73f * MathF.PI / 180f);
		var current = Quaternion.Normalize(yaw * calibration);

		var solved = DieFaceOrientationSolver.SolveNearestFromFaceOrientation(
			calibration,
			current,
			Vector3.UnitY);

		Assert.True(MathF.Abs(Quaternion.Dot(current, solved)) > 0.9999f);
		AssertFacePointsUp(calibration, solved);
	}

	[Fact]
	public void SolveNearestFromFaceOrientation_CorrectsTiltWithoutAddingUnrelatedYaw()
	{
		var calibration = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
		var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 40f * MathF.PI / 180f);
		var tilt = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 35f * MathF.PI / 180f);
		var current = Quaternion.Normalize(tilt * yaw * calibration);

		var solved = DieFaceOrientationSolver.SolveNearestFromFaceOrientation(
			calibration,
			current,
			Vector3.UnitY);
		var arbitraryYaw = DieFaceOrientationSolver.SolveFromFaceOrientation(
			calibration,
			Vector3.UnitY,
			220f);

		AssertFacePointsUp(calibration, solved);
		Assert.True(AngularDistance(current, solved) < AngularDistance(current, arbitraryYaw));
	}
	private static void AssertFacePointsUp(Quaternion calibration, Quaternion solved)
	{
		var localFaceNormal = Vector3.Transform(Vector3.UnitY, Quaternion.Inverse(calibration));
		var worldFaceNormal = Vector3.Normalize(Vector3.Transform(localFaceNormal, solved));
		Assert.True(Vector3.Dot(worldFaceNormal, Vector3.UnitY) > 0.999f);
	}

	private static float AngularDistance(Quaternion a, Quaternion b) =>
		2f * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(
			Quaternion.Normalize(a),
			Quaternion.Normalize(b))), 0f, 1f));
}
