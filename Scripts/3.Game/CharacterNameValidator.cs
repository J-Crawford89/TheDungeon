#nullable enable
using System.Text;

public static class CharacterNameValidator
{
	public const int MaxLength = 32;

	public static string Sanitize(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		var sb = new StringBuilder(Math.Min(text.Length, MaxLength));
		foreach (var c in text)
		{
			if (sb.Length >= MaxLength)
				break;
			if (IsAllowedCharacter(c))
				sb.Append(c);
		}
		return sb.ToString();
	}

	public static bool IsNonEmptyValid(string text) =>
		!string.IsNullOrWhiteSpace(Sanitize(text));

	private static bool IsAllowedCharacter(char c)
	{
		if (c == ' ' || c == '-' || c == '\'')
			return true;
		return char.IsLetter(c);
	}
}
