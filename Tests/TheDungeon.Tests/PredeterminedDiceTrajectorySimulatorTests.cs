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


	[Theory]
	[InlineData(30)]
	[InlineData(60)]
	[InlineData(144)]
	public void Simulate_CompressedPlaybackPreservesTheExactRequestedFinalFace(
		int framesPerSecond)
	{
		var request = CreateD6Request(5);
		var trajectory = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];
		var visibleDuration = PredeterminedTrajectoryPlaybackSampler.ResolveVisibleDuration(
			trajectory.DurationSeconds,
			0.75f);
		var elapsed = 0f;
		while (elapsed < visibleDuration)
		{
			_ = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
				trajectory.Frames,
				elapsed,
				visibleDuration);
			elapsed += 1f / framesPerSecond;
		}

		var finalFrame = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
			trajectory.Frames,
			visibleDuration,
			visibleDuration);
		var displayedNormal = Vector3.Transform(
			request.FaceNormals[request.DesiredFace],
			finalFrame.Orientation);

		Assert.Equal(trajectory.Frames[^1], finalFrame);
		Assert.True(Vector3.Dot(displayedNormal, Vector3.UnitY) > 0.98f);
	}
	[Fact]
	public void Simulate_HeadOnBatchRecordsContactDeflectionAndDisplaysEachRequestedFace()
	{
		var firstTemplate = CreateD6Request(1);
		var secondTemplate = CreateD6Request(6);
		var firstVelocity = new Vector3(7.5f, 0f, 0f);
		var secondVelocity = new Vector3(-7.5f, 0f, 0f);
		var first = firstTemplate with
		{
			StartPosition = new Vector3(-0.75f, firstTemplate.StartPosition.Y, 0f),
			LinearVelocity = firstVelocity,
			AngularVelocity = DieTossKinematicsBuilder.Build(
				firstTemplate.CollisionPoints,
				firstTemplate.StartOrientation,
				firstVelocity,
				0.99f,
				Vector3.UnitY,
				0.6f).AngularVelocity,
		};
		var second = secondTemplate with
		{
			StartPosition = new Vector3(0.75f, secondTemplate.StartPosition.Y, 0f),
			LinearVelocity = secondVelocity,
			AngularVelocity = DieTossKinematicsBuilder.Build(
				secondTemplate.CollisionPoints,
				secondTemplate.StartOrientation,
				secondVelocity,
				0.99f,
				Vector3.UnitY,
				0.6f).AngularVelocity,
		};

		var simulation = PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics([first, second]);
		var results = simulation.Trajectories;

		Assert.Equal(2, results.Count);
		var contacts = simulation.PairContacts
			.Where(contact => contact.FirstDieIndex == 0 && contact.SecondDieIndex == 1)
			.ToArray();
		var commonFrameCount = Math.Min(results[0].Frames.Count, results[1].Frames.Count);
		var minimumDistance = Enumerable.Range(0, commonFrameCount)
			.Min(index => Vector3.Distance(
				results[0].Frames[index].Position,
				results[1].Frames[index].Position));
		Assert.True(
			contacts.Length > 0,
			$"No die contact was recorded; minimum origin distance was {minimumDistance:F3}; " +
			$"manifolds={simulation.ManifoldCallbackCount}, dynamic={simulation.DynamicPairManifoldCount}, " +
			$"identified={simulation.IdentifiedDiePairManifoldCount}.");
		Assert.True(
			contacts.Max(contact => contact.ApproachSpeedBefore) > 5f,
			$"Maximum recorded approach speed was " +
			$"{contacts.Max(contact => contact.ApproachSpeedBefore):F3}.");
		Assert.True(
			contacts.Max(contact => Vector3.Distance(
				contact.FirstVelocityBefore - contact.SecondVelocityBefore,
				contact.FirstVelocityAfter - contact.SecondVelocityAfter)) > 1f,
			"The contact constraint did not measurably deflect the pair.");
		Assert.Contains(contacts, contact => contact.MaximumDepth >= -0.1f);
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
		Assert.InRange(result.AverageContactSlipSpeed, 0f, 1f);
		Assert.InRange(result.FinalContactSlipSpeed, 0f, 0.2f);
		Assert.True(result.AccumulatedRotationRadians >= 3f);
		Assert.True(result.UpwardFaceTransitions >= 1);
		Assert.True(
			CountUpFaceTransitions(request, result) >= 3,
			"The d6 did not visibly tumble through at least three face transitions.");
	}
	[Fact]
	public void Simulate_VariedCoupledD6ThrowsFindNaturalStableCandidatesWithinFourAttempts()
	{
		var template = CreateD6Request(4);
		for (var i = 0; i < 12; i++)
		{
			PredeterminedDieTrajectory? result = null;
			for (var attempt = 0; attempt < 4; attempt++)
			{
				var orientation = Quaternion.CreateFromYawPitchRoll(
					0.31f * i + 0.37f * attempt,
					0.47f + 0.19f * i + 0.23f * attempt,
					0.23f * i + 0.17f * attempt);
				var directionAngle = 0.52f * i + 0.41f * attempt;
				var speed = 6f + i % 4;
				var direction = new Vector3(
					MathF.Cos(directionAngle),
					0f,
					MathF.Sin(directionAngle));
				var velocity = direction * speed;
				var coupling = 0.98f + 0.005f * (i % 5);
				var tumbleAngle = 0.71f * i + 0.29f * attempt;
				var tumbleAxis = Vector3.Normalize(
					direction * MathF.Cos(tumbleAngle) +
					Vector3.UnitY * MathF.Sin(tumbleAngle));
				var kinematics = DieTossKinematicsBuilder.Build(
					template.CollisionPoints,
					orientation,
					velocity,
					coupling,
					tumbleAxis,
					1.25f * ((i + attempt) % 3) / 2f);
				var startPosition = new Vector3(
					-1.5f + 0.25f * i,
					0f,
					(i % 3 - 1) * 0.5f);
				startPosition.Y = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
					template.CollisionPoints,
					orientation,
					floorHeight: 0f,
					clearance: 0.05f);
				var request = template with
				{
					StartPosition = startPosition,
					StartOrientation = orientation,
					LinearVelocity = velocity,
					AngularVelocity = kinematics.AngularVelocity,
				};
				var candidate = PredeterminedDiceTrajectorySimulator.Simulate([request])[0];
				if (!candidate.SettledNaturally ||
					candidate.FinalUpwardFaceDot < 0.95f ||
					candidate.UpwardFaceTransitions < 1)
					continue;
				result = candidate;
				break;
			}

			Assert.NotNull(result);
			Assert.True(
				result.TimeToGripSeconds <= 1f,
				$"Throw {i}: grip={result.TimeToGripSeconds:F3}, " +
				$"average slip={result.AverageContactSlipSpeed:F3}, " +
				$"final slip={result.FinalContactSlipSpeed:F3}.");
			Assert.True(
				result.AverageContactSlipSpeed <= 1f,
				$"Throw {i} averaged {result.AverageContactSlipSpeed:F3} slip speed.");
			Assert.True(
				result.FinalContactSlipSpeed <= 0.2f,
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
	internal static PredeterminedDieThrowRequest CreateD6Request(int desiredFace)
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
			rollCoupling: 0.99f,
			Vector3.UnitY,
			tumbleSpeed: 0.6f);

		var startPosition = new Vector3(-4f, 0f, -1f);
		startPosition.Y = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
			points,
			startOrientation,
			floorHeight: 0f,
			clearance: 0.05f);

		return new PredeterminedDieThrowRequest(
			points,
			normals,
			orientations,
			desiredFace,
			startPosition,
			startOrientation,
			linearVelocity,
			kinematics.AngularVelocity,
			0.25f);
	}
}
