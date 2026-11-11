namespace Trailblazor.BrowserTests;

// Two runs, such as one in each worktree, can overlap only if no host takes a fixed port.
public class BrowserFixtureTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Fact]
    public async Task Fixture_ShouldStartOnOtherPorts_WhenAnotherIsAlreadyServing()
    {
        var second = new BrowserFixture();
        try
        {
            await second.InitializeAsync();

            Uri[] first = [Fixture.BaseAddress, Fixture.DocsAddress, Fixture.TrimmedAppAddress];
            Uri[] other = [second.BaseAddress, second.DocsAddress, second.TrimmedAppAddress];
            Assert.Empty(first.Select(address => address.Port).Intersect(other.Select(address => address.Port)));
        }
        finally
        {
            await second.DisposeAsync();
        }
    }
}
