#nullable enable
using System.Collections.Generic;

public static class PlayerAttackRollBuilder
{
	public static DiceRollRequest BuildToHitRequest(
		PlayerAttackRollInput input,
		PlayerState player,
		string diceRollLabel,
		int targetDefense)
	{
		var ctx = new AttackRollResolutionContext { Attack = input.Attack, Weapon = input.Weapon };
		var hitAbility = input.Attack.AbilityScore;
		var addAbilityToDamage = input.Attack.AddAbilityScoreToDamage;
		input.AbilityOverlay?.Apply(ctx, ref hitAbility, ref addAbilityToDamage);

		var abilityMod = player.AbilityScores.GetScore(hitAbility);
		var modifiers = new List<ModifierWithSource>();
		if (abilityMod != 0)
			modifiers.Add(new ModifierWithSource { Modifier = abilityMod, Source = hitAbility.ToString() });

		if (input.Weapon != null)
		{
			var prof = WeaponProficiencyResolver.Resolve(player.Proficiencies, input.Weapon);
			if ((int)prof.Rank != 0)
				modifiers.Add(new ModifierWithSource { Modifier = (int)prof.Rank, Source = prof.ModifierSourceLabel });
		}
		else
		{
			var profRes = UnarmedProficiencyResolver.Resolve(player.Proficiencies, input.Attack.Id);
			if ((int)profRes.Rank != 0)
				modifiers.Add(new ModifierWithSource { Modifier = (int)profRes.Rank, Source = profRes.ModifierSourceLabel });
		}

		if (input.Attack.AttackModifier != 0)
			modifiers.Add(new ModifierWithSource { Modifier = input.Attack.AttackModifier, Source = "Attack" });

		return new DiceRollRequest
		{
			DiceRollLabel = diceRollLabel,
			TargetNumber = targetDefense,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true },
			},
			ModifiersWithSources = modifiers,
		};
	}

	/// <summary>Resolves ability used on damage after overlay (must match to-hit overlay call).</summary>
	public static (AbilityScore damageAbility, bool addAbilityToDamage) ResolveDamageAbility(PlayerAttackRollInput input)
	{
		var ctx = new AttackRollResolutionContext { Attack = input.Attack, Weapon = input.Weapon };
		var hitAbility = input.Attack.AbilityScore;
		var addAbilityToDamage = input.Attack.AddAbilityScoreToDamage;
		input.AbilityOverlay?.Apply(ctx, ref hitAbility, ref addAbilityToDamage);
		return (hitAbility, addAbilityToDamage);
	}

	public static int RollDamageTotal(PlayerAttackDamageRollInput input, PlayerState player, out string damageDetail)
	{
		var result = RollDamage(input, player);
		damageDetail = result.Detail;
		return result.Total;
	}

	public static PlayerAttackDamageRollResult RollDamage(PlayerAttackDamageRollInput input, PlayerState player)
	{
		var dice = input.Dice;
		var roll = input.Roll;
		var attack = roll.Attack;
		var abilityScore = input.AddAbilityToDamage ? player.AbilityScores.GetScore(input.DamageAbility) : 0;
		var visualDice = new List<PhysicalDieRollSpec>();

		if (string.Equals(attack.Id, DefaultUnarmedAttackDefinition.Id, System.StringComparison.Ordinal))
		{
			var d6 = dice.RollDie(DieType.d6);
			var dmg = CombatFormulas.UnarmedDamageTotal(d6.RolledValue, abilityScore);
			visualDice.Add(ToVisualSpec(d6));
			return new PlayerAttackDamageRollResult
			{
				Total = dmg,
				Detail = $"½×d6 from {d6.RolledValue}",
				VisualDice = visualDice,
			};
		}

		var parts = new List<string>();
		var sum = 0;
		foreach (var comp in attack.DamageComponents)
		{
			var rolled = 0;
			for (var i = 0; i < comp.DamageDice.NumberOfDice; i++)
			{
				var die = dice.RollDie(comp.DamageDice.DieType);
				rolled += die.RolledValue;
				visualDice.Add(ToVisualSpec(die));
			}

			var part = rolled + comp.FlatAmount;
			sum += part;
			parts.Add($"{comp.DamageType.Name} {FormatRolledDiceAndFlat(rolled, comp.FlatAmount)}");
		}

		sum += abilityScore;
		var raw = System.Math.Max(1, sum);
		var abilityLabel = input.DamageAbility.ToString();
		if (parts.Count > 0)
		{
			var detail = abilityScore != 0
				? $"{string.Join(", ", parts)} + {abilityLabel} {abilityScore}"
				: string.Join(", ", parts);
			return new PlayerAttackDamageRollResult { Total = raw, Detail = detail, VisualDice = visualDice };
		}

		return new PlayerAttackDamageRollResult
		{
			Total = raw,
			Detail = abilityScore != 0 ? $"{abilityLabel} {abilityScore}" : "",
			VisualDice = visualDice,
		};
	}

	private static PhysicalDieRollSpec ToVisualSpec(DieRollResult die) => new()
	{
		Kind = DieRollVisualKind.Player,
		DieType = die.DieType,
		Role = DieVisualRole.Standard,
		FaceValue = die.RolledValue,
	};

	private static string FormatRolledDiceAndFlat(int rolled, int flatAmount)
	{
		if (flatAmount == 0)
			return rolled.ToString();
		if (flatAmount > 0)
			return $"{rolled}+{flatAmount}";
		return $"{rolled}{flatAmount}";
	}
}
