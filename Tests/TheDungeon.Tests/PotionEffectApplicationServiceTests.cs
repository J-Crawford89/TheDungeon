using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class PotionEffectApplicationServiceTests
{
	private sealed class ItemRepo : IItemDefinitionRepository
	{
		private readonly Dictionary<string, ItemDefinition> _map;

		public ItemRepo(params ItemDefinition[] defs) =>
			_map = defs.ToDictionary(d => d.Id, StringComparer.Ordinal);

		public IReadOnlyList<ItemDefinition> All => _map.Values.ToList();

		public ItemDefinition? TryGetById(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;
			return _map.TryGetValue(id.Trim(), out var d) ? d : null;
		}

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	private static PotionDefinition HealthPotion(params ItemEffectDefinition[] effects) =>
		new()
		{
			Id = InventoryIds.HealthPotion,
			Name = "Health Potion",
			MaxStackSize = 99,
			Effects = effects.ToList()
		};

	private static PotionEffectApplicationService Service(IItemDefinitionRepository repo, int seed = 1)
	{
		var dice = new DiceRollService(new Random(seed));
		return new PotionEffectApplicationService(dice, new NarrativeService(), repo);
	}

	[Fact]
	public void TryUseHealthPotion_WhenNoneLeft_ReturnsNoneLeft()
	{
		var session = new GameSessionState();
		var svc = Service(new ItemRepo());

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.NoneLeft, result);
	}

	[Fact]
	public void TryUseHealthPotion_WhenAtFullHealth_ReturnsAtFullHealth()
	{
		var session = new GameSessionState();
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 1
		});
		var svc = Service(new ItemRepo());

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.AtFullHealth, result);
	}

	[Fact]
	public void TryUseHealthPotion_WhenDefinitionMissing_ReturnsCannotResolveDefinition()
	{
		var session = new GameSessionState();
		session.Player.CurrentHp = 5;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 1
		});
		var svc = Service(new ItemRepo());

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.CannotResolveDefinition, result);
	}

	[Fact]
	public void TryUseHealthPotion_AppliesHealAndConsumesOne()
	{
		var session = new GameSessionState();
		session.Player.CurrentHp = 4;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 2
		});
		var potionDef = HealthPotion(new RestoreHealthEffectDefinition
		{
			FlatHealAmount = 3,
			HealDice = new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false }
		});
		var svc = Service(new ItemRepo(potionDef));

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.Applied, result);
		Assert.Equal(7, session.Player.CurrentHp);
		Assert.Equal(1, session.Player.InventoryState.SumQuantityForDefinitionId(InventoryIds.HealthPotion));
	}

	[Fact]
	public void TryUseHealthPotion_ClampsToMissingHp()
	{
		var session = new GameSessionState();
		session.Player.CurrentHp = 9;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 1
		});
		var potionDef = HealthPotion(new RestoreHealthEffectDefinition
		{
			FlatHealAmount = 20,
			HealDice = new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false }
		});
		var svc = Service(new ItemRepo(potionDef));

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.Applied, result);
		Assert.Equal(10, session.Player.CurrentHp);
	}

	[Fact]
	public void TryUseHealthPotion_WithNegativeHeal_TotalTreatsAsZero()
	{
		var session = new GameSessionState();
		session.Player.CurrentHp = 5;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 1
		});
		var potionDef = HealthPotion(new RestoreHealthEffectDefinition
		{
			FlatHealAmount = -10,
			HealDice = new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false }
		});
		var svc = Service(new ItemRepo(potionDef));

		var result = svc.TryUseHealthPotion(session);

		Assert.Equal(HealthPotionUseOutcome.Applied, result);
		Assert.Equal(5, session.Player.CurrentHp);
	}
}
