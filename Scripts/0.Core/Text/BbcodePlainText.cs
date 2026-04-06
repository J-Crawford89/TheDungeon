#nullable enable
public static class BbcodePlainText
{
	public static string SanitizeForEmbedding(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		return text.Replace("[", "(").Replace("]", ")");
	}
}
