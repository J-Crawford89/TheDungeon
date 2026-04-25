using System;

public static class UiTextFormatHelper
{
	public static string FromIdToDisplayName(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return id;

		var cleaned = id.Trim().Replace('_', ' ');
		return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
	}
}
