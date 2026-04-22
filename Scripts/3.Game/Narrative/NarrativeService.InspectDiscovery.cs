public sealed partial class NarrativeService
{
	public string ForInspectSurveyRollLabel() => "Survey the room";

	public string ForInspectSurveyRollSummary(int total, string detailText) =>
		$"Survey the room: {total} ({detailText})";

	/// <summary>Player-facing line when a previously hidden feature is noticed. Failures emit no line — naming unrevealed content would be spoilery.</summary>
	public string ForInspectDiscoverReveal(
		string kindLabel,
		string featureName,
		int discoverDc,
		int rollTotal,
		ResolutionOutcome outcome)
	{
		var vs = $"{rollTotal} vs DC {discoverDc}";
		return $"You notice {kindLabel.ToLowerInvariant()}: {featureName} ({vs}, {outcome}).";
	}
}
