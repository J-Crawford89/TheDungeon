/// <summary>Maps a d100 total to percentile-tens and ones-d10 face values for 3D presentation.</summary>
public static class PercentileDiceFaceMapper
{
	public sealed record PercentileFaces(int PercentileTensFace, int OnesDieFace);

	/// <summary>
	/// Percentile die shows 00, 10, 20, …; ones die shows 1–10 (10 represents 0 in the ones place).
	/// 100 → 00 + 10; 30 → 30 + 10; 6 → 00 + 6.
	/// </summary>
	public static PercentileFaces Map(int total)
	{
		var clamped = System.Math.Clamp(total, 1, 100);
		var tens = clamped % 100 / 10 * 10;
		var onesDigit = clamped % 10;
		var onesFace = onesDigit == 0 ? 10 : onesDigit;
		return new PercentileFaces(tens, onesFace);
	}
}
