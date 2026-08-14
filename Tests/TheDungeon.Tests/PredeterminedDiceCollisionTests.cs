using System;
using System.Linq;
using System.Numerics;
using Xunit;

public sealed class PredeterminedDiceCollisionTests
{
	[Fact]
	public void Simulate_GlancingD6PairRecordsContactAndPreservesDisplayedHullOccupancy()
	{
		var pair = CreateConvergingPair(
			PredeterminedDiceTrajectorySimulatorTests.CreateD6Request(2),
			PredeterminedDiceTrajectorySimulatorTests.CreateD6Request(5),
			halfSeparationX: 0.75f,
			halfOffsetZ: 0.18f);

		var simulation = PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics(
			[pair.First, pair.Second]);

		AssertPhysicalContact(simulation, pair.First, pair.Second);
	}

	[Fact]
	public void Simulate_HeadOnD10PairRecordsContactAndPreservesDisplayedHullOccupancy()
	{
		var pair = CreateConvergingPair(
			D10PredeterminedTrajectoryTests.CreateRequest(2, 0),
			D10PredeterminedTrajectoryTests.CreateRequest(9, 0),
			halfSeparationX: 0.95f,
			halfOffsetZ: 0f);

		var simulation = PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics(
			[pair.First, pair.Second]);

		AssertPhysicalContact(simulation, pair.First, pair.Second);
	}

	[Fact]
	public void Simulate_MixedD6D10PairRecordsContactAndPreservesDisplayedHullOccupancy()
	{
		var pair = CreateConvergingPair(
			PredeterminedDiceTrajectorySimulatorTests.CreateD6Request(4),
			D10PredeterminedTrajectoryTests.CreateRequest(7, 1),
			halfSeparationX: 0.9f,
			halfOffsetZ: 0.08f);

		var simulation = PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics(
			[pair.First, pair.Second]);

		AssertPhysicalContact(simulation, pair.First, pair.Second);
	}

	[Theory]
	[InlineData(30, 0.75f)]
	[InlineData(60, 0.75f)]
	[InlineData(144, 0.75f)]
	[InlineData(30, 1f)]
	[InlineData(60, 1f)]
	[InlineData(144, 1f)]
	public void CompressedSharedPlayback_HeadOnContactNeverSamplesAsPassThrough(
		int framesPerSecond,
		float maximumVisibleSeconds)
	{
		var pair = CreateConvergingPair(
			PredeterminedDiceTrajectorySimulatorTests.CreateD6Request(1),
			PredeterminedDiceTrajectorySimulatorTests.CreateD6Request(6),
			halfSeparationX: 0.75f,
			halfOffsetZ: 0f);
		var simulation = PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics(
			[pair.First, pair.Second]);
		var contact = Assert.Single(
			simulation.PairContacts
				.Where(candidate => candidate is { FirstDieIndex: 0, SecondDieIndex: 1 })
				.GroupBy(candidate => candidate.SimulationStep)
				.OrderBy(group => group.Key)
				.Take(1));
		var contactStep = contact.Key;
		var first = simulation.Trajectories[0];
		var second = simulation.Trajectories[1];
		var recordedDuration = MathF.Max(first.DurationSeconds, second.DurationSeconds);
		var visibleDuration = PredeterminedTrajectoryPlaybackSampler.ResolveVisibleDuration(
			recordedDuration,
			maximumVisibleSeconds);
		var visibleContactTime = contactStep / (float)(first.Frames.Count - 1) * visibleDuration;
		var stopAfterVisibleTime = MathF.Min(
			visibleDuration,
			visibleContactTime + 2f / framesPerSecond);
		var previousSignedSeparation = float.NaN;

		for (var elapsed = 0f; elapsed <= stopAfterVisibleTime; elapsed += 1f / framesPerSecond)
		{
			var progress = visibleDuration <= 0f ? 1f : elapsed / visibleDuration;
			var firstFrame = PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(
				first.Frames,
				progress);
			var secondFrame = PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(
				second.Frames,
				progress);
			var signedSeparation = firstFrame.Position.X - secondFrame.Position.X;
			if (float.IsFinite(previousSignedSeparation))
			{
				Assert.False(
					previousSignedSeparation < -0.05f && signedSeparation > 0.05f,
					$"Playback jumped from pre-contact ordering to pass-through at {framesPerSecond} FPS " +
					$"with a {maximumVisibleSeconds:F2}s cap.");
			}
			previousSignedSeparation = signedSeparation;
		}

		Assert.Equal(
			first.Frames[^1],
			PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
				first.Frames,
				visibleDuration,
				visibleDuration));
		Assert.Equal(
			second.Frames[^1],
			PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
				second.Frames,
				visibleDuration,
				visibleDuration));
	}

	private static (PredeterminedDieThrowRequest First, PredeterminedDieThrowRequest Second)
		CreateConvergingPair(
			PredeterminedDieThrowRequest firstTemplate,
			PredeterminedDieThrowRequest secondTemplate,
			float halfSeparationX,
			float halfOffsetZ)
	{
		var firstVelocity = new Vector3(7.5f, 0f, 0f);
		var secondVelocity = new Vector3(-7.5f, 0f, 0f);
		return (
			WithMotion(
				firstTemplate,
				new Vector3(
					-halfSeparationX,
					firstTemplate.StartPosition.Y,
					-halfOffsetZ),
				firstVelocity),
			WithMotion(
				secondTemplate,
				new Vector3(
					halfSeparationX,
					secondTemplate.StartPosition.Y,
					halfOffsetZ),
				secondVelocity));
	}

	private static PredeterminedDieThrowRequest WithMotion(
		PredeterminedDieThrowRequest template,
		Vector3 position,
		Vector3 velocity) =>
		template with
		{
			StartPosition = position,
			LinearVelocity = velocity,
			AngularVelocity = DieTossKinematicsBuilder.Build(
				template.CollisionPoints,
				template.StartOrientation,
				velocity,
				rollCoupling: 0.99f,
				Vector3.UnitY,
				tumbleSpeed: 0.6f).AngularVelocity,
		};

	private static void AssertPhysicalContact(
		PredeterminedDiceSimulationResult simulation,
		PredeterminedDieThrowRequest firstRequest,
		PredeterminedDieThrowRequest secondRequest)
	{
		var contacts = simulation.PairContacts
			.Where(contact => contact is { FirstDieIndex: 0, SecondDieIndex: 1 })
			.ToArray();
		Assert.NotEmpty(contacts);
		Assert.True(
			contacts.Max(contact => contact.ApproachSpeedBefore) > 0.5f,
			"The intended pair never approached along its recorded contact normal.");
		Assert.True(
			contacts.Max(contact => Vector3.Distance(
				contact.FirstVelocityBefore - contact.SecondVelocityBefore,
				contact.FirstVelocityAfter - contact.SecondVelocityAfter)) > 0.25f,
			"The contact constraint did not measurably deflect the pair.");
		Assert.Contains(contacts, contact => contact.ContactCount > 0);
		Assert.Contains(contacts, contact => contact.MaximumDepth >= -0.1f);

		AssertDisplayedHullOccupancyAtContact(
			firstRequest,
			simulation.Trajectories[0],
			contacts[0].SimulationStep);
		AssertDisplayedHullOccupancyAtContact(
			secondRequest,
			simulation.Trajectories[1],
			contacts[0].SimulationStep);
		AssertRequestedFace(firstRequest, simulation.Trajectories[0]);
		AssertRequestedFace(secondRequest, simulation.Trajectories[1]);
	}

	private static void AssertDisplayedHullOccupancyAtContact(
		PredeterminedDieThrowRequest request,
		PredeterminedDieTrajectory trajectory,
		int contactStep)
	{
		var frame = trajectory.Frames[Math.Min(contactStep, trajectory.Frames.Count - 1)];
		var rawOrientation = Quaternion.Normalize(
			frame.Orientation * Quaternion.Inverse(trajectory.DisplayOffset));
		var scale = request.CollisionPoints.Max(point => point.Length());
		var tolerance = MathF.Max(0.002f, scale * 0.005f);
		var rawPoints = request.CollisionPoints
			.Select(point => frame.Position + Vector3.Transform(point, rawOrientation))
			.ToArray();
		foreach (var point in request.CollisionPoints)
		{
			var displayedPoint = frame.Position + Vector3.Transform(point, frame.Orientation);
			var nearestDistance = rawPoints.Min(candidate => Vector3.Distance(
				candidate,
				displayedPoint));
			Assert.True(
				nearestDistance <= tolerance,
				$"Displayed hull occupancy diverged from the simulated contact hull by " +
				$"{nearestDistance:F4} (tolerance {tolerance:F4}).");
		}
	}

	private static void AssertRequestedFace(
		PredeterminedDieThrowRequest request,
		PredeterminedDieTrajectory trajectory)
	{
		var displayedNormal = Vector3.Transform(
			request.FaceNormals[request.DesiredFace],
			trajectory.Frames[^1].Orientation);
		Assert.True(Vector3.Dot(displayedNormal, Vector3.UnitY) > 0.95f);
	}
}
