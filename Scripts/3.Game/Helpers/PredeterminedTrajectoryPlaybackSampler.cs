using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Samples a complete, already-resolved trajectory on a shorter visible timeline.
/// This changes presentation timing only; it never changes the recorded physics path or result.
/// </summary>
public static class PredeterminedTrajectoryPlaybackSampler
{
	public static float ResolveVisibleDuration(
		float recordedDurationSeconds,
		float maximumVisibleDurationSeconds)
	{
		if (!float.IsFinite(recordedDurationSeconds) || recordedDurationSeconds < 0f)
			throw new ArgumentOutOfRangeException(nameof(recordedDurationSeconds));
		if (!float.IsFinite(maximumVisibleDurationSeconds))
			throw new ArgumentOutOfRangeException(nameof(maximumVisibleDurationSeconds));

		return maximumVisibleDurationSeconds <= 0f
			? recordedDurationSeconds
			: MathF.Min(recordedDurationSeconds, maximumVisibleDurationSeconds);
	}

	public static PredeterminedDieTrajectoryFrame SampleAtElapsedTime(
		IReadOnlyList<PredeterminedDieTrajectoryFrame> frames,
		float elapsedVisibleSeconds,
		float visibleDurationSeconds)
	{
		if (!float.IsFinite(elapsedVisibleSeconds))
			throw new ArgumentOutOfRangeException(nameof(elapsedVisibleSeconds));
		if (!float.IsFinite(visibleDurationSeconds) || visibleDurationSeconds < 0f)
			throw new ArgumentOutOfRangeException(nameof(visibleDurationSeconds));

		var progress = visibleDurationSeconds <= 0f
			? 1f
			: Math.Clamp(elapsedVisibleSeconds / visibleDurationSeconds, 0f, 1f);
		return SampleAtProgress(frames, progress);
	}

	public static PredeterminedDieTrajectoryFrame SampleAtProgress(
		IReadOnlyList<PredeterminedDieTrajectoryFrame> frames,
		float progress)
	{
		if (frames == null)
			throw new ArgumentNullException(nameof(frames));
		if (frames.Count == 0)
			throw new ArgumentException("A trajectory requires at least one frame.", nameof(frames));
		if (!float.IsFinite(progress))
			throw new ArgumentOutOfRangeException(nameof(progress));
		if (frames.Count == 1 || progress <= 0f)
			return frames[0];
		if (progress >= 1f)
			return frames[^1];

		var framePosition = progress * (frames.Count - 1);
		var fromIndex = Math.Clamp((int)MathF.Floor(framePosition), 0, frames.Count - 2);
		var toIndex = fromIndex + 1;
		var weight = framePosition - fromIndex;
		var from = frames[fromIndex];
		var to = frames[toIndex];
		return new PredeterminedDieTrajectoryFrame(
			Vector3.Lerp(from.Position, to.Position, weight),
			Quaternion.Normalize(Quaternion.Slerp(
				Quaternion.Normalize(from.Orientation),
				Quaternion.Normalize(to.Orientation),
				weight)));
	}
}
