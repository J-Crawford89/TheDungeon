using System.Collections.Generic;

public static class MonsterLibrary 
{
	public static readonly MonsterDefinition Rat = new()
	{
		Id = "rat",
		Name = "Rat",
		MaxHp = 2,
		Attack = 1,
		Defense = 12,
		RandomizerWeight = 50
	};

	public static readonly MonsterDefinition GiantRat = new()
	{
		Id = "giant_rat",
		Name = "Giant Rat",
		MaxHp = 20,
		Attack = 4,
		Defense = 10,
		RandomizerWeight = 20
	};

	public static readonly MonsterDefinition RatKing = new()
	{
		Id = "rat_king",
		Name = "Rat King",
		MaxHp = 32,
		Attack = 6,
		Defense = 13,
		IsBoss = true,
		RandomizerWeight = 5
	};

	public static IReadOnlyList<MonsterDefinition> All => new[]
	{
		Rat,
		GiantRat,
		RatKing
	};
}
