using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class CombatInitiative
{
	private readonly DiceRollService _dice;
	private readonly NarrativeService _narrative;
	private readonly ResolutionService? _resolution;

	public CombatInitiative(DiceRollService dice, NarrativeService narrative, ResolutionService? resolution = null)
	{
		_dice = dice;
		_narrative = narrative;
		_resolution = resolution;
	}

	public List<string> BuildTurnOrderNames(MonsterFeature feature, List<CombatTurnSlot> order)
	{
		var names = new List<string>();
		foreach (var slot in order)
			names.Add(slot.IsPlayer ? "You" : feature.Monsters[slot.MonsterIndex].Definition.Name);
		return names;
	}

	public async Task<List<CombatTurnSlot>> RollInitiativeOrderAsync(
		GameSessionState session,
		MonsterFeature feature,
		CancellationToken ct = default)
	{
		var entries = new List<InitiativeEntry>();

		var playerAgi = session.Player.AbilityScores.Agility;
		var pRoll = _dice.RollD20Plus("Initiative (you)", playerAgi, "Agility");
		await PresentInitiativeRollAsync(pRoll, DieRollVisualKind.Player, ct);
		entries.Add(new InitiativeEntry { Total = pRoll.Total, Agility = playerAgi, IsPlayer = true, MonsterIndex = -1 });
		session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll("You", pRoll) });
		await NotifyResolvedRollAsync(ct);

		for (var i = 0; i < feature.Monsters.Count; i++)
		{
			if (feature.Monsters[i].CurrentHp <= 0)
				continue;
			var monsterAgi = 0;
			var mRoll = _dice.RollD20Plus($"Initiative ({feature.Monsters[i].Definition.Name})", monsterAgi, "Agility");
			await PresentInitiativeRollAsync(mRoll, DieRollVisualKind.Monster, ct);
			entries.Add(new InitiativeEntry { Total = mRoll.Total, Agility = monsterAgi, IsPlayer = false, MonsterIndex = i });
			session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll(feature.Monsters[i].Definition.Name, mRoll) });
			await NotifyResolvedRollAsync(ct);
		}

		entries.Sort(InitiativeHelper.Compare);
		return entries.Select(e => e.IsPlayer
				? new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 }
				: new CombatTurnSlot { IsPlayer = false, MonsterIndex = e.MonsterIndex })
			.ToList();
	}

	private Task PresentInitiativeRollAsync(
		DiceRollResult roll,
		DieRollVisualKind kind,
		CancellationToken ct) =>
		_resolution == null
			? Task.CompletedTask
			: _resolution.PresentRollVisualAsync(
				roll,
				new DiceRollRequest(),
				kind,
				DicePresentationProfile.Standard,
				ct);

	private Task NotifyResolvedRollAsync(CancellationToken ct) =>
		_resolution == null
			? Task.CompletedTask
			: _resolution.NotifyResolvedRollAsync(ct);
}
