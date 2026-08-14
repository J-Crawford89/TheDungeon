#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed partial class NarrativeService
{
	public string ForHarvestRollAttempt(
		string itemName,
		int attemptNumber,
		int quantity,
		int total,
		int targetNumber,
		bool succeeded,
		string detail)
	{
		var outcome = succeeded ? "success" : "failure";
		var detailSuffix = string.IsNullOrWhiteSpace(detail)
			? string.Empty
			: $" ({detail.Trim()})";
		return $"Harvest ({itemName}) [{attemptNumber}/{quantity}]: " +
			$"total {total} vs DC {targetNumber} - {outcome}{detailSuffix}";
	}

	/// <summary>Single roll-log line bundling every per-unit attempt for one stack.</summary>
	public string ForHarvestRollBundle(string itemName, int quantity, IReadOnlyList<string> perUnitDetailLines)
	{
		if (perUnitDetailLines == null || perUnitDetailLines.Count == 0)
			return $"Harvest ({itemName}) ×{quantity}";
		var joined = string.Join("; ", perUnitDetailLines.Where(static s => !string.IsNullOrWhiteSpace(s)));
		return $"Harvest ({itemName}) ×{quantity}: {joined}";
	}

	public string ForHarvestStackOutcome(string containerKindLabel, string itemName, int successes, int failures)
	{
		if (successes <= 0 && failures > 0)
			return failures == 1
				? $"You ruin the {itemName} trying to extract it from the {containerKindLabel}."
				: $"You ruin {failures} × {itemName} trying to extract them from the {containerKindLabel}.";
		if (failures <= 0 && successes > 0)
			return ForLootTakenFromContainer(containerKindLabel, itemName, successes);
		var total = successes + failures;
		return $"From the {containerKindLabel}, you recover {successes} of {total} × {itemName}; the rest are lost.";
	}
}
