using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

public sealed class D10PredeterminedTrajectoryTests
{
	[Fact]
	public void Simulate_AlignedD10DisplaysOneUnambiguousPhysicalFace()
	{
		var successfulAttempt = -1;
		for (var attempt = 0; attempt < 8; attempt++)
		{
			var candidate = PredeterminedDiceTrajectorySimulator.Simulate(
				[CreateRequest(1, attempt)])[0];
			if (!candidate.SettledNaturally || candidate.FinalUpwardFaceDot < 0.95f)
				continue;
			successfulAttempt = attempt;
			break;
		}
		Assert.True(successfulAttempt >= 0, "No stable physical d10 face was found in eight natural candidates.");

		for (var desiredFace = 1; desiredFace <= 10; desiredFace++)
		{
			var request = CreateRequest(desiredFace, successfulAttempt);
			var result = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];

			Assert.True(result.SettledNaturally);
			Assert.Equal(desiredFace, result.DisplayedFace);
			var finalOrientation = result.Frames[^1].Orientation;
			var desiredDot = DotWithUp(request.FaceNormals[desiredFace], finalOrientation);
			var secondBestDot = request.FaceNormals
				.Where(entry => entry.Key != desiredFace)
				.Max(entry => DotWithUp(entry.Value, finalOrientation));
			Assert.True(
				desiredDot > 0.95f,
				$"Face {desiredFace} desired dot was {desiredDot:F4}; neighbor {secondBestDot:F4}; " +
				$"natural={result.NaturalFace}, settled={result.SettledNaturally}.");
			Assert.True(
				secondBestDot < 0.75f,
				$"Face {desiredFace} was ambiguous; neighboring face dot was {secondBestDot:F3}.");
		}
	}

	private static float DotWithUp(Vector3 localNormal, Quaternion orientation) =>
		Vector3.Dot(
			Vector3.Normalize(Vector3.Transform(localNormal, orientation)),
			Vector3.UnitY);

	internal static PredeterminedDieThrowRequest CreateRequest(int desiredFace, int attempt)
	{
		var points = new List<Vector3>
		{
			new(-0.2534808f, -0.071887195f, -0.77986634f),
			new(-0.0001604557f, -0.68f, -0.0001526475f),
			new(-0.82f, -0.071887195f, -0.0001526475f),
			new(-0.6634808f, 0.07175416f, -0.48199648f),
			new(0.2533204f, 0.07175416f, -0.77986634f),
			new(0.66332036f, -0.071887195f, -0.48199648f),
			new(-0.2534808f, -0.071887195f, 0.77986634f),
			new(0.66332036f, -0.071887195f, 0.48184383f),
			new(-0.6634808f, 0.07175416f, 0.48184383f),
			new(-0.0001604557f, 0.68f, -0.0001526475f),
			new(0.8200001f, 0.07175416f, -0.0001526475f),
			new(0.2533204f, 0.07175416f, 0.77986634f),
		};
		for (var i = 0; i < points.Count; i++)
			points[i] *= 1.01f;
		var approximateOrientations = new Dictionary<int, Quaternion>
		{
			[1] = new(0.33948f, 0f, 0.467255f, 0.816349f),
			[2] = new(0f, 0f, 0.816349f, 0.577559f),
			[3] = new(0.549291f, 0f, -0.178475f, 0.816349f),
			[4] = new(-0.776394f, 0f, 0.252266f, 0.577559f),
			[5] = new(0f, 0f, -0.577559f, 0.816349f),
			[6] = new(-0.479838f, 0f, -0.66044f, 0.577559f),
			[7] = new(-0.549291f, 0f, -0.178475f, 0.816349f),
			[8] = new(0.479838f, 0f, -0.66044f, 0.577559f),
			[9] = new(-0.33948f, 0f, 0.467255f, 0.816349f),
			[10] = new(0.776394f, 0f, 0.252266f, 0.577559f),
		};
		var aligned = DieFaceHullAlignment.Align(points, approximateOrientations);
		var startOrientation = Quaternion.CreateFromYawPitchRoll(
			0.41f + 0.37f * attempt,
			0.97f + 0.23f * attempt,
			0.28f + 0.19f * attempt);
		var directionAngle = 0.43f * attempt;
		var velocity = new Vector3(
			8f * MathF.Cos(directionAngle),
			0f,
			8f * MathF.Sin(directionAngle));
		var kinematics = DieTossKinematicsBuilder.Build(
			points,
			startOrientation,
			velocity,
			rollCoupling: 0.99f,
			Vector3.UnitY,
			tumbleSpeed: 0.6f);
		var startPosition = new Vector3(-3f, 0f, -0.5f);
		startPosition.Y = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
			points,
			startOrientation,
			floorHeight: 0f,
			clearance: 0.05f);

		return new PredeterminedDieThrowRequest(
			points,
			aligned.FaceNormals,
			aligned.FaceUpOrientations,
			desiredFace,
			startPosition,
			startOrientation,
			velocity,
			kinematics.AngularVelocity,
			0.25f);
	}
}
