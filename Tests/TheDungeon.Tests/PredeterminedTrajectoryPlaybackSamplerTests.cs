using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

public sealed class PredeterminedTrajectoryPlaybackSamplerTests
{
	[Theory]
	[InlineData(2.4f, 0.75f, 0.75f)]
	[InlineData(0.6f, 0.75f, 0.6f)]
	[InlineData(2.4f, 0f, 2.4f)]
	[InlineData(2.4f, -1f, 2.4f)]
	public void ResolveVisibleDuration_CapsOnlyWhenEnabled(
		float recorded,
		float maximum,
		float expected) =>
		Assert.Equal(expected, PredeterminedTrajectoryPlaybackSampler.ResolveVisibleDuration(
			recorded,
			maximum), 5);

	[Fact]
	public void SampleAtProgress_ReturnsExactEndpointFrames()
	{
		var frames = CreateFrames();

		Assert.Equal(frames[0], PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(frames, 0f));
		Assert.Equal(frames[^1], PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(frames, 1f));
		Assert.Equal(frames[^1], PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(frames, 2f));
	}

	[Fact]
	public void SampleAtProgress_InterpolatesPositionAndNormalizedOrientation()
	{
		var frames = CreateFrames();

		var sample = PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(frames, 0.25f);

		Assert.Equal(new Vector3(5f, 1f, -2f), sample.Position);
		Assert.InRange(sample.Orientation.Length(), 0.9999f, 1.0001f);
		var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.25f);
		Assert.True(MathF.Abs(Quaternion.Dot(expected, sample.Orientation)) > 0.9999f);
	}

	[Theory]
	[InlineData(30)]
	[InlineData(60)]
	[InlineData(144)]
	public void SampleAtElapsedTime_AlwaysFinishesOnExactFinalPose(int framesPerSecond)
	{
		var frames = CreateFrames();
		const float visibleDuration = 0.75f;
		var elapsed = 0f;
		var sample = frames[0];
		while (elapsed < visibleDuration)
		{
			sample = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
				frames,
				elapsed,
				visibleDuration);
			elapsed += 1f / framesPerSecond;
		}

		sample = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
			frames,
			visibleDuration,
			visibleDuration);

		Assert.Equal(frames[^1], sample);
	}

	[Fact]
	public void SampleAtElapsedTime_UsesSameProgressForEveryDieInBatch()
	{
		var firstFrames = CreateFrames();
		var secondFrames = new List<PredeterminedDieTrajectoryFrame>
		{
			new(new Vector3(100f, 0f, 0f), Quaternion.Identity),
			new(new Vector3(120f, 0f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI)),
			new(new Vector3(140f, 0f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI * 2f)),
		};

		var first = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
			firstFrames,
			0.3f,
			0.75f);
		var second = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(
			secondFrames,
			0.3f,
			0.75f);

		Assert.Equal(8f, first.Position.X, 4);
		Assert.Equal(116f, second.Position.X, 4);
	}

	[Fact]
	public void SampleAtElapsedTime_DisabledZeroDurationReturnsFinalPose()
	{
		var frames = CreateFrames();

		var sample = PredeterminedTrajectoryPlaybackSampler.SampleAtElapsedTime(frames, 0f, 0f);

		Assert.Equal(frames[^1], sample);
	}

	[Fact]
	public void InvalidInputs_AreRejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			PredeterminedTrajectoryPlaybackSampler.ResolveVisibleDuration(-0.1f, 0.75f));
		Assert.Throws<ArgumentException>(() =>
			PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(
				Array.Empty<PredeterminedDieTrajectoryFrame>(),
				0.5f));
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			PredeterminedTrajectoryPlaybackSampler.SampleAtProgress(CreateFrames(), float.NaN));
	}

	private static IReadOnlyList<PredeterminedDieTrajectoryFrame> CreateFrames() =>
	[
		new(new Vector3(0f, 1f, -2f), Quaternion.Identity),
		new(new Vector3(10f, 1f, -2f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.5f)),
		new(new Vector3(20f, 1f, -2f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI)),
	];
}
