using System;
using System.Numerics;
using Xunit;

namespace TheDungeon.Tests;

public sealed class DieFaceOrientationSolverTests
{
	[Fact]
	public void SolveFromFaceOrientation_SpinChangesResultButFaceStillTowardCamera()
	{
		var calibration = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
		var current = Quaternion.Identity;
		var cameraDir = Vector3.UnitZ;

		var a = DieFaceOrientationSolver.SolveFromFaceOrientation(calibration, current, cameraDir, 0f);
		var b = DieFaceOrientationSolver.SolveFromFaceOrientation(calibration, current, cameraDir, 45f);

		Assert.NotEqual(a, b);

		var faceUp = Vector3.Transform(Vector3.UnitY, calibration);
		var worldA = Vector3.Normalize(Vector3.Transform(faceUp, a));
		var worldB = Vector3.Normalize(Vector3.Transform(faceUp, b));
		Assert.True(Vector3.Dot(worldA, cameraDir) > 0.95f);
		Assert.True(Vector3.Dot(worldB, cameraDir) > 0.95f);
	}
}
