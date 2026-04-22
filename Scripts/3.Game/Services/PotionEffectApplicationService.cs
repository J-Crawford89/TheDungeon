using System.Collections.Generic;

/// <summary>Applies consumable potion effects from definitions (usable outside combat).
/// </summary>
public sealed class PotionEffectApplicationService
{
	private readonly DiceRollService _dice;
	private readonly NarrativeService _narrative;
	private readonly IItemDefinitionRepository _items;

	public PotionEffectApplicationService(
		DiceRollService dice,
		NarrativeService narrative,
		IItemDefinitionRepository items)
	{
		_dice = dice;
		_narrative = narrative;
		_items = items;
	}

	/// <summary>Use one health potion stack from inventory (constant id).
	/// Does not enforce combat-only; callers decide context.
	/// </summary>
	public HealthPotionUseOutcome TryUseHealthPotion(GameSessionState session)
	{
		var player = session.Player;
		var potionItemId = InventoryIds.HealthPotion;

		if (player.InventoryState.SumQuantityForDefinitionId(potionItemId) <= 0)
		{
			session.AppendGameLog(_narrative.ForHealthPotionNoneLeft());
			return HealthPotionUseOutcome.NoneLeft;
		}

		if (player.CurrentHp >= player.MaxHp)
		{
			session.AppendGameLog(_narrative.ForHealthPotionAtFullHealth());
			return HealthPotionUseOutcome.AtFullHealth;
		}

		var catalogDef = _items.TryGetById(potionItemId);
		if (catalogDef is not PotionDefinition potionDefinition)
		{
			session.AppendGameLog(_narrative.ForHealthPotionNoneLeft());
			return HealthPotionUseOutcome.CannotResolveDefinition;
		}

		var healTotal = ComputeHealFromPotion(session, potionDefinition);
		var missing = player.MaxHp - player.CurrentHp;
		var raw = healTotal < 0 ? 0 : healTotal;
		var heal = raw <= missing ? raw : missing;

		if (!player.InventoryState.TryConsumeOne(potionItemId))
		{
			session.AppendGameLog(_narrative.ForHealthPotionNoneLeft());
			return HealthPotionUseOutcome.NoneLeft;
		}

		player.CurrentHp += heal;
		session.AppendGameLog(_narrative.ForUsedHealthPotion(heal, player.CurrentHp));

		return HealthPotionUseOutcome.Applied;
	}

	private int ComputeHealFromPotion(GameSessionState session, PotionDefinition potion)
	{
		var sum = 0;
		foreach (var effect in potion.Effects)
		{
			if (effect is not RestoreHealthEffectDefinition rh)
				continue;

			sum += rh.FlatHealAmount;
			var expr = rh.HealDice;
			if (expr.NumberOfDice <= 0)
				continue;

			var req = new DiceRollRequest
			{
				DiceRollLabel = $"{potion.Name ?? "Potion"} heal dice",
				TargetNumber = 0,
				CheckStyle = D20CheckStyle.None,
				DiceExpressions = new List<DiceExpression> { expr },
				ModifiersWithSources = new List<ModifierWithSource>()
			};
			var roll = _dice.Roll(req);
			sum += roll.RollTotal;

			session.AppendLog(new LogEntry
			{
				Kind = LogEntryKind.Roll,
				Text = roll.DetailText
			});
		}

		return sum;
	}
}
