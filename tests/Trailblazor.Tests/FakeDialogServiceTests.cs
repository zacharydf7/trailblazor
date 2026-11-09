using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests;

public class FakeDialogServiceTests : BunitContext
{
    public FakeDialogServiceTests() => this.AddTrailblazor();

    private FakeDialogService Dialogs => Services.GetRequiredService<FakeDialogService>();

    [Fact]
    public void AddTrailblazor_ShouldMakeTheFakeTheDialogService_WhenCalled() =>
        Assert.Same(Dialogs, Services.GetRequiredService<IDialogService>());

    [Fact]
    public async Task Last_ShouldGiveTheDialogAClickOpenedToCheckAndClose_WhenItIsStillOpen()
    {
        var page = Render<TrailPage>();

        var click = page.Find("[data-testid=rename]").ClickAsync(new());
        var rename = Dialogs.Last<RenameDialog, string>();
        Assert.Equal(("Rename trail", "Pine Hollow"), (rename.Title, rename.Parameters[nameof(RenameDialog.Name)]));
        await rename.CloseAsync("Eagle Ridge");
        await click;

        Assert.Equal("Renamed to Eagle Ridge", page.Find("output").TextContent);
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Theory]
    [InlineData(true, "Deleted")]
    [InlineData(false, "Kept")]
    public async Task Answer_ShouldConfirmOrCancelAtOnce_WhenSet(bool answer, string status)
    {
        Dialogs.Answer = _ => answer;
        var page = Render<TrailPage>();

        await page.Find("[data-testid=delete]").ClickAsync(new());

        Assert.Equal(status, page.Find("output").TextContent);
        var confirm = Assert.Single(Dialogs.Opened);
        Assert.Equal(("Delete trail?", "This can't be undone."), (confirm.Title, confirm.Parameters["Message"]));
        Assert.Empty(Dialogs.OpenDialogs);
    }

    [Fact]
    public async Task Answer_ShouldLeaveTheConfirmOpenForTheTestToAnswer_WhenItAnswersNull()
    {
        Dialogs.Answer = dialog => dialog.Title == "Delete trail?" ? null : false;
        var page = Render<TrailPage>();

        var click = page.Find("[data-testid=delete]").ClickAsync(new());

        Assert.False(click.IsCompleted);
        var confirm = Dialogs.LastConfirm();
        Assert.Same(confirm, Assert.Single(Dialogs.OpenDialogs));
        await confirm.CloseAsync(true);
        await click;
        Assert.Equal("Deleted", page.Find("output").TextContent);
    }

    [Fact]
    public async Task Answer_ShouldCloseAnAlert_WhenItAnswersFalse()
    {
        Dialogs.Answer = _ => false;
        var page = Render<TrailPage>();

        await page.Find("[data-testid=notify]").ClickAsync(new());

        Assert.Equal("Told", page.Find("output").TextContent);
        Assert.Equal("Trail closed", Assert.Single(Dialogs.Opened).Title);
    }

    [Fact]
    public void Last_ShouldThrow_WhenNoDialogOfTheKindWasOpened()
    {
        Dialogs.Show<OtherDialog, string>("Other");

        var error = Assert.Throws<InvalidOperationException>(() => Dialogs.Last<RenameDialog, string>());
        var noConfirm = Assert.Throws<InvalidOperationException>(() => Dialogs.LastConfirm());

        Assert.Equal("No RenameDialog was opened.", error.Message);
        Assert.Equal("No confirm or alert was opened.", noConfirm.Message);
    }

    [Fact]
    public async Task Opened_ShouldKeepEveryDialogInOrder_WhenSomeHaveClosed()
    {
        var first = Dialogs.Show<RenameDialog, string>("First");
        var second = Dialogs.ShowDynamic<string>(typeof(OtherDialog), "Second");

        await first.CancelAsync();

        Assert.Equal([first, second], Dialogs.Opened);
        Assert.Equal([second], Dialogs.OpenDialogs);
        Assert.Same(first, Dialogs.Last<RenameDialog, string>());
    }

    [Fact]
    public async Task ShowAsync_ShouldRecordTheDialogForTheTestToClose_WhenItHasNoResult()
    {
        var showing = Dialogs.ShowAsync<NoticeDialog>("Trail notice");

        var notice = Dialogs.Last<NoticeDialog>();
        await notice.CloseAsync(true);

        Assert.True(await showing);
        Assert.Equal("Trail notice", Assert.Single(Dialogs.Opened).Title);
    }

    private sealed class TrailPage : ComponentBase
    {
        private string _status = "";

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            AddButton(builder, 0, "rename", async () =>
            {
                var result = await DialogService.ShowAsync<RenameDialog, string>("Rename trail",
                    parameters => parameters.Set(rename => rename.Name, "Pine Hollow"));
                _status = $"Renamed to {result.Value}";
            });
            AddButton(builder, 10, "delete", async () =>
                _status = await DialogService.ConfirmAsync("Delete trail?", "This can't be undone.") ? "Deleted"
                    : "Kept");
            AddButton(builder, 20, "notify", async () =>
            {
                await DialogService.AlertAsync("Trail closed", "Pine Hollow is closed for the winter.");
                _status = "Told";
            });
            builder.OpenElement(30, "output");
            builder.AddContent(31, _status);
            builder.CloseElement();
        }

        private void AddButton(RenderTreeBuilder builder, int sequence, string testId, Func<Task> onClick)
        {
            builder.OpenElement(sequence, "button");
            builder.AddAttribute(sequence + 1, "data-testid", testId);
            builder.AddAttribute(sequence + 2, "onclick", EventCallback.Factory.Create(this, onClick));
            builder.CloseElement();
        }
    }

    private sealed class RenameDialog : TbDialogContent<string>
    {
        [Parameter]
        public string Name { get; set; } = "";
    }

    private sealed class OtherDialog : TbDialogContent<string>;

    private sealed class NoticeDialog : TbDialogContent;
}
