using System.Collections.Generic;
using System.Linq;

public sealed partial class NarrativeService
{
	public string ForCombatStarted(IReadOnlyList<string> livingMonsterNames)
	{
		if (livingMonsterNames.Count == 0)
			return "You thought you saw a goblin, but it was your shadow.";
		var parts = livingMonsterNames
			.Select((name, i) => (name, i))
			.GroupBy(x => x.name)
			.OrderBy(g => g.Min(x => x.i))
			.Select(g =>
			{
				var n = g.Count();
				return n == 1 ? g.Key : $"{n} {g.Key}";
			});
		return $"Combat begins! Facing: {string.Join(", ", parts)}.";
	}

	public string ForCombatInitiativeRoll(string who, DiceRollResult roll) =>
		$"{who} initiative: {roll.SummaryText}. {roll.DetailText}".Trim();

	public string ForCombatTurnOrderSummary(IReadOnlyList<string> namesInOrder) =>
		$"Turn order: {string.Join(" → ", namesInOrder)}.";

	public string ForAttackRoll(string attacker, string target, int totalVsAc, int targetNumber, string detail) =>
		$"{attacker} vs {target} (need {targetNumber}+): rolled {totalVsAc}. {detail}".Trim();

	public string ForAttackMiss(string attacker, string target) =>
		$"{attacker} misses {target}.";

	public string ForDamageDealt(string targetName, int damage, int hpRemaining, int d6Face) =>
		$"You hit {targetName} for {damage} damage (½×d6 from {d6Face}). {targetName} has {hpRemaining} HP left.";

	public string ForMonsterHitPlayer(string monsterName, int damage, int playerHp) =>
		$"{monsterName} hits you for {damage} damage. You have {playerHp} HP left.";

	public string ForFleeRoll(int total, int fleeDc, string detail) =>
		$"Flee attempt: {total} vs DC {fleeDc}. {detail}".Trim();

	public string ForFleeSuccess() => "You break away and retreat to the previous room!";

	public string ForFleeFailure() => "You fail to escape!";

	public string ForCombatVictory() => "You are victorious! The threats here are finished.";
}
