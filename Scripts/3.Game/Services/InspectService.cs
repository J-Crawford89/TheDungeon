#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Inspect actions: surveying the room to reveal hidden trap/treasure/npc/lore instances.</summary>
public sealed class InspectService
{
	private readonly IDiceRollRequestExecutor _dice;
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;

	public InspectService(
		IDiceRollRequestExecutor dice,
		ResolutionService resolution,
		NarrativeService narrative)
	{
		_dice = dice;
		_resolution = resolution;
		_narrative = narrative;
	}

	/// <summary>
	/// Increments <see cref="DungeonRoom.InspectAttemptCount"/>.
	/// If hidden discoverable instances exist, rolls once and compares that roll to each instance's DiscoverDc.
	/// </summary>
	public void RunInspectDiscovery(GameSessionState session, DungeonRoom room)
		=> RunInspectDiscoveryAsync(session, room).GetAwaiter().GetResult();

	public async Task RunInspectDiscoveryAsync(GameSessionState session, DungeonRoom room)
	{
		room.InspectAttemptCount++;

		var candidates = GatherHiddenCandidates(room);
		if (candidates.Count == 0)
			return;

		var scores = session.Player.AbilityScores;
		var useIntelligence = scores.Intelligence >= scores.Wisdom;
		var mod = useIntelligence ? scores.Intelligence : scores.Wisdom;
		var sourceLabel = useIntelligence ? "Intelligence" : "Wisdom";

		var req = new DiceRollRequest
		{
			DiceRollLabel = _narrative.ForInspectSurveyRollLabel(),
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true },
			},
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = mod, Source = sourceLabel },
			},
		};

		var diceRoll = _dice.Roll(req);
		await _resolution.PresentRollVisualAsync(
			diceRoll,
			req,
			DieRollVisualKind.Player,
			DicePresentationProfile.Standard);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForInspectSurveyRollSummary(diceRoll.Total, diceRoll.DetailText),
		});

		foreach (var c in candidates)
		{
			var outcome = _resolution.ResolveOutcomeAgainstTarget(diceRoll, c.DiscoverDc);
			var revealed = outcome is ResolutionOutcome.Success or ResolutionOutcome.CriticalSuccess;
			if (!revealed)
				continue;

			c.MarkRevealed();
			session.AppendGameLog(_narrative.ForInspectDiscoverReveal(c.KindLabel, c.Name, c.DiscoverDc, diceRoll.Total, outcome));
		}

		await _resolution.NotifyResolvedRollAsync();
	}

	private sealed class HiddenCandidate
	{
		public required string KindLabel { get; init; }
		public required string Name { get; init; }
		public required int DiscoverDc { get; init; }
		public required Action MarkRevealed { get; init; }
	}

	private List<HiddenCandidate> GatherHiddenCandidates(DungeonRoom room)
	{
		var list = new List<HiddenCandidate>();

		foreach (var feature in room.Features)
		{
			switch (feature)
			{
				case TrapFeature tf:
					foreach (var t in tf.Traps.Where(x => !x.IsRevealed && x.Definition.DiscoverDc > 0))
					{
						var dc = t.Definition.DiscoverDc;
						var trap = t;
						list.Add(new HiddenCandidate
						{
							KindLabel = "Trap",
							Name = trap.Definition.Name,
							DiscoverDc = dc,
							MarkRevealed = () => trap.IsRevealed = true,
						});
					}

					break;
				case TreasureFeature treasure:
					foreach (var inst in treasure.TreasureItems.Where(x => !x.IsRevealed && x.Definition.DiscoverDc > 0))
					{
						var dc = inst.Definition.DiscoverDc;
						var ti = inst;
						list.Add(new HiddenCandidate
						{
							KindLabel = "Treasure",
							Name = ti.Definition.Name,
							DiscoverDc = dc,
							MarkRevealed = () => ti.IsRevealed = true,
						});
					}

					break;
				case NpcFeature nf:
					foreach (var n in nf.NPCs.Where(x => !x.IsRevealed && x.Definition.DiscoverDc > 0))
					{
						var dc = n.Definition.DiscoverDc;
						var ni = n;
						list.Add(new HiddenCandidate
						{
							KindLabel = "Figure",
							Name = ni.Definition.Name,
							DiscoverDc = dc,
							MarkRevealed = () => ni.IsRevealed = true,
						});
					}

					break;
				case LoreFeature lf:
					foreach (var l in lf.Lore.Where(x => !x.IsRevealed && x.Definition.DiscoverDc > 0))
					{
						var dc = l.Definition.DiscoverDc;
						var li = l;
						list.Add(new HiddenCandidate
						{
							KindLabel = "Clue",
							Name = l.Definition.Name,
							DiscoverDc = dc,
							MarkRevealed = () => li.IsRevealed = true,
						});
					}

					break;
			}
		}

		return list;
	}
}
