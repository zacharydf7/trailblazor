using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbDialogContentHostTests : BunitContext
{
    public TbDialogContentHostTests() => this.AddTrailblazor();

    [Fact]
    public void Host_ShouldShowTheContentWithItsParametersAndTitle_WhenRendered()
    {
        var cut = RenderHost(parameters => parameters.Set(rename => rename.Name, "Pine Hollow"));

        Assert.Equal("Pine Hollow", cut.Find("[data-testid=name]").TextContent);
        Assert.Equal("Rename trail", cut.Find("[data-testid=title]").TextContent);
        Assert.False(cut.Instance.Result.IsCompleted);
    }

    [Fact]
    public void HasWindow_ShouldTellTheContentWhereItShows_WhenInAHostOrOpenedAsADialog()
    {
        var hosted = RenderHost().FindComponent<RenameContent>().Instance;

        var opened = Services.GetRequiredService<IDialogService>().Show<RenameContent, string>();

        Assert.False(hosted.HasWindow);
        Assert.True(opened.HasWindow);
    }

    [Fact]
    public async Task Host_ShouldGiveTheValueOnceAndRenderNothing_WhenTheContentCloses()
    {
        var closed = new List<DialogResult<string>>();
        var cut = RenderHost(parameters => parameters.Set(rename => rename.Name, "Pine Hollow"), closed.Add);

        var save = cut.Find("[data-testid=save]");
        await save.ClickAsync(new());

        var result = await cut.Instance.Result;
        Assert.Equal((true, "Pine Hollow!"), (result.Confirmed, result.Value));
        Assert.Equal([result], closed);
        Assert.Equal("", cut.Markup);
    }

    [Fact]
    public async Task Host_ShouldGiveACanceledResult_WhenTheContentCancels()
    {
        var closed = new List<DialogResult<string>>();
        var cut = RenderHost(onClosed: closed.Add);

        await cut.Find("[data-testid=cancel]").ClickAsync(new());

        Assert.False((await cut.Instance.Result).Confirmed);
        Assert.False(Assert.Single(closed).Confirmed);
    }

    [Fact]
    public async Task Host_ShouldCloseOnlyOnce_WhenTheContentClosesTwice()
    {
        var closed = new List<DialogResult<string>>();
        var cut = RenderHost(onClosed: closed.Add);
        var content = cut.FindComponent<RenameContent>().Instance;

        await cut.InvokeAsync(() => Task.WhenAll(content.SaveAsync(), content.SaveAsync()));

        Assert.Single(closed);
    }

    [Fact]
    public async Task Host_ShouldCloseAndTellItsParent_WhenTheContentClosesFromAnotherThread()
    {
        var closed = new List<DialogResult<string>>();
        var cut = RenderHost(onClosed: closed.Add);
        var content = cut.FindComponent<RenameContent>().Instance;

        await Task.Run(content.SaveAsync, Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Equal("", cut.Markup));
        Assert.True(Assert.Single(closed).Confirmed);
    }

    [Fact]
    public async Task Result_ShouldFinishAsCanceled_WhenTheHostGoesAwayWithItsContentOpen()
    {
        var closed = new List<DialogResult<string>>();
        var cut = RenderHost(onClosed: closed.Add);
        var result = cut.Instance.Result;

        await DisposeComponentsAsync();

        Assert.False((await result).Confirmed);
        Assert.Empty(closed);
    }

    [Fact]
    public async Task DismissAsync_ShouldLeaveTheAnswerToTheContent_WhenCalled()
    {
        var cut = RenderHost(parameters => parameters.Set(rename => rename.KeepOpen, true));

        await cut.InvokeAsync(() => cut.Instance.DismissAsync());

        // The content kept itself open, as it would keep a dialog open on Escape.
        Assert.False(cut.Instance.Result.IsCompleted);
        Assert.Equal("CloseButton", cut.Find("[data-testid=asked]").TextContent);

        cut.Instance.Update(parameters => parameters.Set(rename => rename.KeepOpen, false));
        await cut.InvokeAsync(() => cut.Instance.DismissAsync(DialogDismissReason.Escape));

        Assert.False((await cut.Instance.Result).Confirmed);
    }

    [Fact]
    public void Update_ShouldSetTheContentsParameters_WhenCalled()
    {
        var cut = RenderHost(parameters => parameters.Set(rename => rename.Name, "Pine Hollow"));

        cut.Instance.Update(parameters => parameters.Set(rename => rename.Name, "Eagle Ridge"));

        cut.WaitForAssertion(() => Assert.Equal("Eagle Ridge", cut.Find("[data-testid=name]").TextContent));
    }

    [Fact]
    public void Title_ShouldTellTheParentTheContentsOwnTitle_WhenNoneIsGiven()
    {
        var titles = new List<string?>();

        var cut = Render<TbDialogContentHost<RenameContent, string>>(parameters =>
            parameters.Add(host => host.TitleChanged, titles.Add));

        cut.WaitForAssertion(() => Assert.Equal(["Rename trail"], titles));
    }

    [Fact]
    public void Title_ShouldBeTheContentsTitle_WhenGiven()
    {
        var titles = new List<string?>();

        var cut = Render<TbDialogContentHost<RenameContent, string>>(parameters =>
            parameters.Add(host => host.Title, "Rename Pine Hollow").Add(host => host.TitleChanged, titles.Add));

        Assert.Equal("Rename Pine Hollow", cut.Find("[data-testid=title]").TextContent);
        // The parent gave the title, so it isn't told its own.
        Assert.Empty(titles);
    }

    [Fact]
    public async Task Title_ShouldFollowTheContentAndTheParent_WhenEitherChangesIt()
    {
        var titles = new List<string?>();
        var cut = Render<TbDialogContentHost<RenameContent, string>>(parameters =>
            parameters.Add(host => host.Title, "Rename Pine Hollow").Add(host => host.TitleChanged, titles.Add));
        var content = cut.FindComponent<RenameContent>().Instance;

        await cut.InvokeAsync(() => content.Retitle("Rename Eagle Ridge"));

        cut.WaitForAssertion(() => Assert.Equal(["Rename Eagle Ridge"], titles));

        // The parent renders again with the title it gave before, which doesn't undo the content's.
        cut.Render(parameters => parameters.Add(host => host.Title, "Rename Pine Hollow"));

        Assert.Equal("Rename Eagle Ridge", content.Title);

        cut.Render(parameters => parameters.Add(host => host.Title, "Rename Bear Creek"));

        Assert.Equal("Rename Bear Creek", content.Title);
        Assert.Equal(["Rename Eagle Ridge"], titles);
    }

    [Fact]
    public async Task Host_ShouldCloseWithItsResult_WhenItSitsInADockPanel()
    {
        var closed = new List<DialogResult<string>>();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.AriaLabel, "Workbench")
            .Add(dock => dock.ChildContent, builder =>
            {
                builder.OpenComponent<TbDockPanel>(0);
                builder.AddAttribute(1, nameof(TbDockPanel.Id), "rename");
                builder.AddAttribute(2, nameof(TbDockPanel.Title), "Rename");
                builder.AddAttribute(3, nameof(TbDockPanel.ChildContent), (RenderFragment)(panel =>
                {
                    panel.OpenComponent<TbDialogContentHost<RenameContent, string>>(0);
                    panel.AddAttribute(1, nameof(TbDialogContentHost<RenameContent, string>.OnClosed),
                        EventCallback.Factory.Create<DialogResult<string>>(this, closed.Add));
                    panel.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        await cut.Find("[data-part=panel] [data-testid=save]").ClickAsync(new());

        Assert.Equal("!", Assert.Single(closed).Value);
        Assert.Empty(cut.FindAll("[data-part=panel] [data-testid=save]"));
    }

    private IRenderedComponent<TbDialogContentHost<RenameContent, string>> RenderHost(
        Action<DialogParameters<RenameContent>>? parameters = null, Action<DialogResult<string>>? onClosed = null) =>
        Render<TbDialogContentHost<RenameContent, string>>(host =>
        {
            host.Add(each => each.Parameters, parameters);
            if (onClosed is not null)
            {
                host.Add(each => each.OnClosed, onClosed);
            }
        });

    // Content written for a dialog: it closes with the name and an exclamation mark, cancels, and can refuse to be
    // dismissed.
    private sealed class RenameContent : TbDialogContent<string>
    {
        private string _asked = "";

        [Parameter]
        public string Name { get; set; } = "";

        [Parameter]
        public bool KeepOpen { get; set; }

        protected override string DefaultTitle => "Rename trail";

        public Task SaveAsync() => Dialog.CloseAsync($"{Name}!");

        public string Title => Dialog.Title;

        public bool HasWindow => Dialog.HasWindow;

        public void Retitle(string title) => Dialog.Title = title;

        protected override Task OnDismissAsync(DialogDismissReason reason)
        {
            _asked = reason.ToString();
            StateHasChanged();
            return KeepOpen ? Task.CompletedTask : base.OnDismissAsync(reason);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "data-testid", "name");
            builder.AddContent(2, Name);
            builder.CloseElement();
            builder.OpenElement(3, "p");
            builder.AddAttribute(4, "data-testid", "title");
            builder.AddContent(5, Dialog.Title);
            builder.CloseElement();
            builder.OpenElement(6, "p");
            builder.AddAttribute(7, "data-testid", "asked");
            builder.AddContent(8, _asked);
            builder.CloseElement();
            builder.OpenElement(9, "button");
            builder.AddAttribute(10, "data-testid", "save");
            builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(this, SaveAsync));
            builder.CloseElement();
            builder.OpenElement(12, "button");
            builder.AddAttribute(13, "data-testid", "cancel");
            builder.AddAttribute(14, "onclick", EventCallback.Factory.Create(this, Dialog.CancelAsync));
            builder.CloseElement();
        }
    }
}
