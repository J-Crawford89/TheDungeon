using System;

public static class DisplayIdHelper
{
	/// <summary>
	/// Replaces underscores with spaces, then capitalizes the first character of each word
	/// (e.g. <c>raw_damage_type</c> → <c>Raw Damage Type</c>).
	/// </summary>
	public static string FormatSnakeOrRawId(string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return "";

		var s = id.Trim().Replace('_', ' ');
		var words = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		if (words.Length == 0)
			return "";

		for (var i = 0; i < words.Length; i++)
		{
			var w = words[i];
			if (w.Length == 0)
				continue;
			words[i] = w.Length == 1
				? w.ToUpperInvariant()
				: char.ToUpperInvariant(w[0]) + w[1..];
		}

		return string.Join(" ", words);
	}
}
