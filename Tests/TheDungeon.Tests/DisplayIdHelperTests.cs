using Xunit;

public sealed class DisplayIdHelperTests
{
	[Fact]
	public void FormatSnakeOrRawId_NullOrWhitespace_ReturnsEmpty()
	{
		Assert.Equal("", DisplayIdHelper.FormatSnakeOrRawId(null));
		Assert.Equal("", DisplayIdHelper.FormatSnakeOrRawId("  "));
	}

	[Fact]
	public void FormatSnakeOrRawId_SnakeCase_ToTitleWords()
	{
		Assert.Equal("Raw Damage Type", DisplayIdHelper.FormatSnakeOrRawId("raw_damage_type"));
	}

	[Fact]
	public void FormatSnakeOrRawId_SingleLetterWords_AreUppercase()
	{
		Assert.Equal("A B", DisplayIdHelper.FormatSnakeOrRawId("a_b"));
	}
}
