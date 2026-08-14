public enum DicePresentationProfile
{
	Standard = 0,
	RapidSequence = 1,
}

public readonly record struct DicePresentationTiming(
	float PlaybackDurationMultiplier,
	float ResultPauseMultiplier)
{
	public static DicePresentationTiming For(DicePresentationProfile profile) =>
		profile switch
		{
			DicePresentationProfile.RapidSequence => new DicePresentationTiming(0.75f, 0.5f),
			_ => new DicePresentationTiming(1f, 1f),
		};
}
