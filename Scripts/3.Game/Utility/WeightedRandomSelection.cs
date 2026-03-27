using System;
using System.Collections.Generic;
using System.Linq;

public static class WeightedRandomSelection
{
	public static T Pick<T>(Random random, IEnumerable<(T item, double weight)> weightedItems)
	{
		var valid = weightedItems.Where(x => x.weight > 0).ToList();
		if (valid.Count == 0)
			throw new ArgumentException("At least one item must have a positive weight.", nameof(weightedItems));

		var total = valid.Sum(x => x.weight);
		var r = random.NextDouble() * total;
		foreach (var (item, weight) in valid)
		{
			r -= weight;
			if (r <= 0)
				return item;
		}

		return valid[^1].item;
	}
}
