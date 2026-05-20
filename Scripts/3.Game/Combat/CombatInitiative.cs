using System.Collections.Generic;
using System.Linq;

public sealed class CombatInitiative
{
	private readonly DiceRollService _dice;
	private readonly NarrativeService _narrative;
	private readonly DiceRollPresenterHost? _dicePresenterHost;

	public CombatInitiative(DiceRollService dice, NarrativeService narrative, DiceRollPresenterHost? dicePresenterHost = null)
	{
		_dice = dice;
		_narrative = narrative;
		_dicePresenterHost = dicePresenterHost;
	}

	public List<string> BuildTurnOrderNames(MonsterFeature feature, List<CombatTurnSlot> order)
	{
		var names = new List<string>();
		foreach (var slot in order)
			names.Add(slot.IsPlayer ? "You" : feature.Monsters[slot.MonsterIndex].Definition.Name);
		return names;
	}

	public List<CombatTurnSlot> RollInitiativeOrder(GameSessionState session, MonsterFeature feature)
	{
		var entries = new List<InitiativeEntry>();

		var playerAgi = session.Player.AbilityScores.Agility;
		var pRoll = _dice.RollD20Plus("Initiative (you)", playerAgi, "Agility");
		PresentInitiativeRoll(pRoll, DieRollVisualKind.Player);
		session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll("You", pRoll) });
		entries.Add(new InitiativeEntry { Total = pRoll.Total, Agility = playerAgi, IsPlayer = true, MonsterIndex = -1 });

		for (var i = 0; i < feature.Monsters.Count; i++)
		{
			if (feature.Monsters[i].CurrentHp <= 0)
				continue;
			var monsterAgi = 0;
			var mRoll = _dice.RollD20Plus($"Initiative ({feature.Monsters[i].Definition.Name})", monsterAgi, "Agility");
			PresentInitiativeRoll(mRoll, DieRollVisualKind.Monster);
			session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll(feature.Monsters[i].Definition.Name, mRoll) });
			entries.Add(new InitiativeEntry { Total = mRoll.Total, Agility = monsterAgi, IsPlayer = false, MonsterIndex = i });
		}

		entries.Sort(InitiativeHelper.Compare);
		return entries.Select(e => e.IsPlayer
				? new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 }
				: new CombatTurnSlot { IsPlayer = false, MonsterIndex = e.MonsterIndex })
			.ToList();
	}

	private void PresentInitiativeRoll(DiceRollResult roll, DieRollVisualKind kind)
	{
		if (_dicePresenterHost == null)
			return;
		DiceRollPresentation.PresentResultFireAndForget(_dicePresenterHost.Presenter, roll, kind);
	}
}
