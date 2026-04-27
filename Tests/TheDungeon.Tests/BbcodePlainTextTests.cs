using Xunit;

public sealed class BbcodePlainTextTests
{
	[Fact]
	public void SanitizeForEmbedding_NullOrEmpty_ReturnsEmpty()
	{
		Assert.Equal("", BbcodePlainText.SanitizeForEmbedding(null));
		Assert.Equal("", BbcodePlainText.SanitizeForEmbedding(""));
	}

	[Fact]
	public void SanitizeForEmbedding_ReplacesSquareBrackets()
	{
		Assert.Equal("(b)Bold(/b)", BbcodePlainText.SanitizeForEmbedding("[b]Bold[/b]"));
	}
}
