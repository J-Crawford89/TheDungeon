using System;
using System.Linq;
using Xunit;

public sealed class CombatMonsterTurnIncomingDamageTests
{
	[Fact]
	public void ExecuteMonsterTurn_WhenArmorBandHit_AppliesDamageReductionAndCanClampToZero()
	{
		var foundArmorHit = false;
		for (var seed = 0; seed < 400; seed++)
		{
			var session = CreateSessionWithArmor(
				agility: -10,
				armorBonus: 100,
				damageReduction: new DamageReductionEffectDefinition
				{
					DamageFamily = DamageFamily.Physical,
					ReductionAmount = 100
				});
			var feature = CreateSingleMonsterFeature(attack: 8);
			var turn = CreateMonsterTurn(seed);
			var hpBefore = session.Player.CurrentHp;

			turn.ExecuteMonsterTurn(session, feature, 0);
			if (!session.LogEntries.Any(e => e.Text.Contains("strikes your armor", StringComparison.Ordinal)))
				continue;

			foundArmorHit = true;
			Assert.Equal(hpBefore, session.Player.CurrentHp);
			break;
		}

		Assert.True(foundArmorHit);
	}

	[Fact]
	public void ExecuteMonsterTurn_WhenDirectHit_DoesNotApplyArmorDamageReduction()
	{
		var foundDirectHit = false;
		for (var seed = 0; seed < 400; seed++)
		{
			var session = CreateSessionWithArmor(
				agility: -10,
				armorBonus: 0,
				damageReduction: new DamageReductionEffectDefinition
				{
					DamageFamily = DamageFamily.Physical,
					ReductionAmount = 20
				});
			var feature = CreateSingleMonsterFeature(attack: 6);
			var turn = CreateMonsterTurn(seed);
			var hpBefore = session.Player.CurrentHp;

			turn.ExecuteMonsterTurn(session, feature, 0);
			if (!session.LogEntries.Any(e => e.Text.Contains("hits you for", StringComparison.Ordinal)))
				continue;

			foundDirectHit = true;
			Assert.True(session.Player.CurrentHp < hpBefore);
			break;
		}

		Assert.True(foundDirectHit);
	}

	private static CombatMonsterTurn CreateMonsterTurn(int seed)
	{
		var resolution = new ResolutionService(new DiceRollService(new Random(seed)));
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		return new CombatMonsterTurn(resolution, narrative, vitals, downed);
	}

	private static GameSessionState CreateSessionWithArmor(
		int agility,
		int armorBonus,
		DamageReductionEffectDefinition damageReduction)
	{
		var session = new GameSessionState();
		var player = session.Player;
		player.CurrentHp = 20;
		player.MaxHp = 20;
		player.AbilityScores = new AbilityScores { Agility = agility };
		var armor = new ArmorDefinition
		{
			Id = "armor",
			Name = "Armor",
			ArmorBonus = armorBonus,
			Effects = [damageReduction]
		};
		var armorInstance = new ItemInstance { Definition = armor, Quantity = 1 };
		player.InventoryState.Items.Add(armorInstance);
		player.InventoryState.EquippedBySlot[EquipmentSlot.Torso] = armorInstance;
		return session;
	}

	private static MonsterFeature CreateSingleMonsterFeature(int attack) =>
		new()
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 10,
					Definition = new MonsterDefinition
					{
						Id = "m",
						Name = "Monster",
						Attack = attack,
						MaxHp = 10,
						Defense = 0,
						ExperienceReward = 0,
						IsBoss = false,
						RandomizerWeight = 1
					}
				}
			]
		};
}

