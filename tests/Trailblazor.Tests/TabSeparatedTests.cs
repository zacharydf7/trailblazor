namespace Trailblazor.Tests;

public class TabSeparatedTests
{
    [Theory]
    [InlineData("a\tb\nc\td", "a|b;c|d")]
    [InlineData("a\tb\r\n", "a|b")]
    [InlineData("\"two\nlines\"\t\"tab\there\"\t\"say \"\"hi\"\"\"", "two\nlines|tab\there|say \"hi\"")]
    [InlineData("a\t\tb\n\n", "a||b;")]
    [InlineData("", "")]
    public void Read_ShouldSplitRowsAndCells_WhenGivenTextFromASpreadsheet(string text, string expected)
    {
        var rows = TabSeparated.Read(text);

        Assert.Equal(expected, string.Join(';', rows.Select(row => string.Join('|', row))));
    }
}
