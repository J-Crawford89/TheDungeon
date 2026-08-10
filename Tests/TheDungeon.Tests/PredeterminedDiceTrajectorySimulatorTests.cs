using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

public sealed class PredeterminedDiceTrajectorySimulatorTests
{
	[Fact]
	public void Simulate_D6DisplaysEveryRequestedFaceWithoutChangingNaturalTrajectory()
	{
		var requests = Enumerable.Range(1, 6)
			.Select(CreateD6Request)
			.ToArray();

		foreach (var request in requests)
		{
			var result = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];

			Assert.True(result.SettledNaturally);
			Assert.Equal(request.DesiredFace, result.DisplayedFace);
			Assert.InRange(result.MaximumAngularSpeed, 0f, 25f);
			var finalOrientation = result.Frames[^1].Orientation;
			var displayedNormal = Vector3.Transform(
				request.FaceNormals[request.DesiredFace],
				finalOrientation);
			Assert.True(
				Vector3.Dot(displayedNormal, Vector3.UnitY) > 0.98f,
				$"Face {request.DesiredFace} was not up: {displayedNormal}.");
		}
	}

	[Fact]
	public void Simulate_RepeatedIdenticalRequestProducesIdenticalTrajectory()
	{
		var request = CreateD6Request(4);

		var first = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];
		var second = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];

		Assert.Equal(first.NaturalFace, second.NaturalFace);
		Assert.Equal(first.Frames.Count, second.Frames.Count);
		for (var i = 0; i < first.Frames.Count; i++)
		{
			Assert.Equal(first.Frames[i].Position, second.Frames[i].Position);
			Assert.Equal(first.Frames[i].Orientation, second.Frames[i].Orientation);
		}
	}

	[Fact]
	public void Simulate_RecordedPlaybackHasNoFinalOrientationJump()
	{
		var result = PredeterminedDiceTrajectorySimulator.Simulate([CreateD6Request(2)])[0];
		var largestStepRadians = 0f;
		for (var i = 1; i < result.Frames.Count; i++)
		{
			var dot = Math.Clamp(MathF.Abs(Quaternion.Dot(
				result.Frames[i - 1].Orientation,
				result.Frames[i].Orientation)), 0f, 1f);
			largestStepRadians = MathF.Max(largestStepRadians, 2f * MathF.Acos(dot));
		}

		Assert.True(
			largestStepRadians < 0.8f,
			$"Largest recorded orientation step was {largestStepRadians} radians.");
	}

	[Fact]
	public void Simulate_BatchPreservesCollisionsAndDisplaysEachRequestedFace()
	{
		var first = CreateD6Request(1) with
		{
			StartPosition = new Vector3(-2.5f, 1.35f, -0.3f),
			LinearVelocity = new Vector3(7.5f, 0f, 0.5f),
		};
		var second = CreateD6Request(6) with
		{
			StartPosition = new Vector3(2.5f, 1.35f, 0.3f),
			LinearVelocity = new Vector3(-7.5f, 0f, -0.5f),
		};

		var results = PredeterminedDiceTrajectorySimulator.Simulate([first, second]);

		Assert.Equal(2, results.Count);
		Assert.All(results, result => Assert.True(result.SettledNaturally));
		for (var i = 0; i < results.Count; i++)
		{
			var request = i == 0 ? first : second;
			var displayedNormal = Vector3.Transform(
				request.FaceNormals[request.DesiredFace],
				results[i].Frames[^1].Orientation);
			Assert.True(Vector3.Dot(displayedNormal, Vector3.UnitY) > 0.98f);
		}
	}

	[Fact]
	public void Simulate_D3RecognizesTheRepeatedValueOnAnOppositeCubeFace()
	{
		var cube = CreateD6Request(1);
		var d3 = cube with
		{
			FaceNormals = cube.FaceNormals.Where(entry => entry.Key <= 3)
				.ToDictionary(entry => entry.Key, entry => entry.Value),
			FaceUpOrientations = cube.FaceUpOrientations.Where(entry => entry.Key <= 3)
				.ToDictionary(entry => entry.Key, entry => entry.Value),
			StartPosition = new Vector3(0f, 1.35f, 0f),
			StartOrientation = Quaternion.Identity,
			LinearVelocity = Vector3.Zero,
			AngularVelocity = Vector3.Zero,
			OppositeFacesShareValues = true,
		};

		var result = PredeterminedDiceTrajectorySimulator.Simulate([d3])[0];

		Assert.True(result.SettledNaturally);
		Assert.Equal(3, result.NaturalFace);
		var displayedNormal = Vector3.Transform(
			d3.FaceNormals[d3.DesiredFace],
			result.Frames[^1].Orientation);
		Assert.True(Vector3.Dot(displayedNormal, Vector3.UnitY) > 0.98f);
	}
	[Fact]
	public void Simulate_CoupledThrowQuicklyGripsInsteadOfSkating()
	{
		var request = CreateD6Request(4);
		var result = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];

		Assert.InRange(result.TimeToGripSeconds, 0f, 0.75f);
		Assert.InRange(result.AverageContactSlipSpeed, 0f, 2f);
		Assert.InRange(result.FinalContactSlipSpeed, 0f, 0.4f);
		Assert.True(result.AccumulatedRotationRadians >= 3f);
		Assert.True(
			CountUpFaceTransitions(request, result) >= 3,
			"The d6 did not visibly tumble through at least three face transitions.");
	}
	[Fact]
	public void Simulate_VariedCoupledD6ThrowsStayWithinGripBounds()
	{
		var template = CreateD6Request(4);
		for (var i = 0; i < 12; i++)
		{
			var orientation = Quaternion.CreateFromYawPitchRoll(
				0.31f * i,
				0.47f + 0.19f * i,
				0.23f * i);
			var directionAngle = 0.52f * i;
			var speed = 6f + i % 4;
			var direction = new Vector3(
				MathF.Cos(directionAngle),
				0f,
				MathF.Sin(directionAngle));
			var velocity = direction * speed;
			var coupling = 0.82f + 0.04f * (i % 5);
			var tumbleAngle = 0.71f * i;
			var tumbleAxis = Vector3.Normalize(
				direction * MathF.Cos(tumbleAngle) +
				Vector3.UnitY * MathF.Sin(tumbleAngle));
			var kinematics = DieTossKinematicsBuilder.Build(
				template.CollisionPoints,
				orientation,
				velocity,
				coupling,
				tumbleAxis,
				1.25f * (i % 3) / 2f);
			var request = template with
			{
				StartPosition = new Vector3(-1.5f + 0.25f * i, 1.35f, (i % 3 - 1) * 0.5f),
				StartOrientation = orientation,
				LinearVelocity = velocity,
				AngularVelocity = kinematics.AngularVelocity,
			};

			var result = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];

			Assert.True(result.SettledNaturally, $"Throw {i} did not settle.");
			Assert.True(
				result.TimeToGripSeconds <= 1f,
				$"Throw {i}: grip={result.TimeToGripSeconds:F3}, " +
				$"average slip={result.AverageContactSlipSpeed:F3}, " +
				$"final slip={result.FinalContactSlipSpeed:F3}.");
			Assert.True(
				result.AverageContactSlipSpeed <= 2f,
				$"Throw {i} averaged {result.AverageContactSlipSpeed:F3} slip speed.");
			Assert.True(
				result.FinalContactSlipSpeed <= 0.4f,
				$"Throw {i} ended at {result.FinalContactSlipSpeed:F3} slip speed.");
			Assert.True(
				result.AccumulatedRotationRadians >= 3f,
				$"Throw {i} accumulated only {result.AccumulatedRotationRadians:F2} radians, " +
				$"duration {result.DurationSeconds:F2}s, max spin {result.MaximumAngularSpeed:F2}.");
		}
	}
	private static int CountUpFaceTransitions(
		PredeterminedDieThrowRequest request,
		PredeterminedDieTrajectory trajectory)
	{
		var transitions = 0;
		var previousFace = -1;
		foreach (var frame in trajectory.Frames)
		{
			var currentFace = request.FaceNormals
				.MaxBy(entry => Vector3.Dot(
					Vector3.Transform(entry.Value, frame.Orientation),
					Vector3.UnitY))
				.Key;
			if (previousFace >= 0 && currentFace != previousFace)
				transitions++;
			previousFace = currentFace;
		}
		return transitions;
	}
	private static PredeterminedDieThrowRequest CreateD6Request(int desiredFace)
	{
		const float extent = 0.57735026f * 1.01f;
		var points = new List<Vector3>(8);
		foreach (var x in new[] { -extent, extent })
		foreach (var y in new[] { -extent, extent })
		foreach (var z in new[] { -extent, extent })
			points.Add(new Vector3(x, y, z));

		var orientations = new Dictionary<int, Quaternion>
		{
			[1] = Quaternion.Normalize(new Quaternion(0f, 0f, -0.707107f, 0.707107f)),
			[2] = Quaternion.Normalize(new Quaternion(-0.707107f, 0f, 0f, 0.707107f)),
			[3] = Quaternion.Normalize(new Quaternion(0f, 0f, 1f, 0f)),
			[4] = Quaternion.Identity,
			[5] = Quaternion.Normalize(new Quaternion(0.707107f, 0f, 0f, 0.707107f)),
			[6] = Quaternion.Normalize(new Quaternion(0f, 0f, 0.707107f, 0.707107f)),
		};
		var normals = orientations.ToDictionary(
			entry => entry.Key,
			entry => Vector3.Transform(Vector3.UnitY, Quaternion.Inverse(entry.Value)));

		var startOrientation = Quaternion.CreateFromYawPitchRoll(0.63f, 1.17f, 0.41f);
		var linearVelocity = new Vector3(8f, 0f, 2f);
		var kinematics = DieTossKinematicsBuilder.Build(
			points,
			startOrientation,
			linearVelocity,
			rollCoupling: 0.9f,
			Vector3.UnitY,
			tumbleSpeed: 0.6f);

		return new PredeterminedDieThrowRequest(
			points,
			normals,
			orientations,
			desiredFace,
			new Vector3(-4f, 1.35f, -1f),
			startOrientation,
			linearVelocity,
			kinematics.AngularVelocity,
			0.25f);
	}
}
