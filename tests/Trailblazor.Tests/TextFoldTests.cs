using System.Globalization;

namespace Trailblazor.Tests;

public class TextFoldTests
{
    [Theory]
    [InlineData("José", "jose")]
    [InlineData("Éagle Ridge", "EAGLE")]
    [InlineData("Pine Hollow", "ollo")]
    [InlineData("ΟΔΟΣ ΑΘΗΝΩΝ", "οδος")]
    [InlineData("مُحَمَّد", "محمد")]
    [InlineData("שָׁלוֹם", "שלום")]
    public void Contains_ShouldIgnoreCaseAndAccents_WhenGivenText(string text, string query)
    {
        Assert.True(TextFold.Contains(text, query));
    }

    [Theory]
    [InlineData("काम", "कम")]
    [InlineData("パン", "ハン")]
    public void Contains_ShouldKeepAnotherScriptsMarks_WhenTheyAreLetters(string text, string query)
    {
        Assert.False(TextFold.Contains(text, query));
    }

    [Fact]
    public void Contains_ShouldFindADottedCapitalI_WhenTheCultureIsTurkish()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.True(TextFold.Contains("İstanbul", "ist"));
            Assert.False(TextFold.Contains("Istanbul", "ist"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
