#nullable enable
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class DamageComponentsMapper
{
	public static List<DamageComponent> ToDomainList(Array<DamageComponentResource>? parts)
	{
		var list = new List<DamageComponent>();
		if (parts == null || parts.Count == 0)
			return list;

		foreach (var part in parts)
		{
			if (part == null)
				continue;
			var mapped = ToDomain(part);
			if (mapped != null)
				list.Add(mapped);
		}

		return list;
	}

	public static DamageComponent? ToDomain(DamageComponentResource? part)
	{
		if (part == null)
			return null;
		if (part.DamageType == null)
			return null;

		var typeId = part.DamageType.Id?.Trim() ?? "";
		if (typeId.Length == 0)
			return null;

		var type = DamageTypeMapper.ToDomain(part.DamageType);
		if (string.IsNullOrWhiteSpace(type.Id))
			return null;

		var dice = new DiceExpression
		{
			NumberOfDice = System.Math.Max(0, part.NumberOfDice),
			DieType = part.NumberOfDice > 0 ? part.DiceType : DieType.d6,
			InD20CheckPool = part.InD20CheckPool,
		};

		return new DamageComponent(dice, part.FlatAmount, type);
	}
}
