using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbValidationSummaryTests : BunitContext
{
    public TbValidationSummaryTests() => this.AddTrailblazor();

    [Fact]
    public async Task TbValidationSummary_ShouldListEachFieldsMessageAsALinkToIt_WhenTheFormFailsToSubmit()
    {
        var cut = Render<ValidationSummaryExample>();
        Assert.Empty(cut.FindAll(".tb-validation-summary"));

        await cut.Find("form").SubmitAsync();

        Assert.Equal(["Name is required.", "Email is required.", "Emergency contact is required."],
            cut.Tb().ValidationSummary().Messages);
        var summary = cut.Find(".tb-validation-summary");
        Assert.Equal("Fix these to continue", summary.QuerySelector("[data-part=title]")!.TextContent);
        Assert.Equal(summary.QuerySelector("[data-part=title]")!.Id, summary.GetAttribute("aria-labelledby"));
        Assert.Equal(cut.FindAll("input").Select(input => $"#{input.Id}"),
            summary.QuerySelectorAll("[data-part=link]").Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public async Task TbValidationSummary_ShouldDropAMessage_WhenItsFieldIsFixed()
    {
        var cut = Render<ValidationSummaryExample>();
        await cut.Find("form").SubmitAsync();

        await cut.Tb().TextField("Email").EnterAsync("robin@example.com");

        Assert.Equal(["Name is required.", "Emergency contact is required."], cut.Tb().ValidationSummary().Messages);
    }

    [Fact]
    public async Task TbValidationSummary_ShouldListTheFormsOwnMessagesWithoutALink_WhenTheyBelongToNoField()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, new Booking())
            .Add(form => form.DataAnnotations, true).Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbValidationSummary>(0);
                builder.AddComponentParameter(1, nameof(TbValidationSummary.Title), "Before you book");
                builder.CloseComponent();
            })));

        await cut.Find("form").SubmitAsync();

        Assert.Equal(["Pick at least one trail."], cut.Tb().ValidationSummary().Messages);
        Assert.Empty(cut.FindAll("[data-part=link]"));
        Assert.Equal("Before you book", cut.Find("[data-part=title]").TextContent);
    }

    [Fact]
    public async Task TbValidationSummary_ShouldKeepAFormMessage_WhenAFieldShowsTheSameText()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, new Booking())
            .Add(form => form.DataAnnotations, true).Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbValidationSummary>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddComponentParameter(2, nameof(TbTextField<string>.Label), "Trail");
                builder.AddComponentParameter(3, nameof(TbTextField<string>.Error), "Pick at least one trail.");
                builder.CloseComponent();
            })));

        await cut.Find("form").SubmitAsync();

        Assert.Equal(["Pick at least one trail.", "Pick at least one trail."], cut.Tb().ValidationSummary().Messages);
        Assert.Single(cut.FindAll("[data-part=link]"));
    }

    [Fact]
    public async Task Link_ShouldPointAtTheGroup_WhenTheFieldIsARadioGroup()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbValidationSummary>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbRadioGroup<string>>(1);
            builder.AddComponentParameter(2, nameof(TbRadioGroup<string>.Label), "Difficulty");
            builder.AddComponentParameter(3, nameof(TbRadioGroup<string>.Required), true);
            builder.AddComponentParameter(4, nameof(TbRadioGroup<string>.ChildContent), (RenderFragment)(radios =>
            {
                radios.OpenComponent<TbRadio<string>>(0);
                radios.AddComponentParameter(1, nameof(TbRadio<string>.Value), "easy");
                radios.AddComponentParameter(2, nameof(TbRadio<string>.Text), "Easy");
                radios.CloseComponent();
            }));
            builder.CloseComponent();
        })));

        await cut.Find("form").SubmitAsync();

        var target = cut.Find("[data-part=link]").GetAttribute("href")![1..];
        Assert.Equal("fieldset", cut.Find($"[id='{target}']").LocalName);
    }

    [Fact]
    public void HeadingLevel_ShouldSetTheHeadingsElement_WhenGiven()
    {
        var cut = Render<ValidationSummaryServerExample>();

        Assert.Equal("H3", cut.Find("[data-part=title]").TagName);
        Assert.Equal("The trail wasn't saved", cut.Find("[data-part=title]").TextContent);
    }

    [Fact]
    public void Render_ShouldListTheMessages_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ValidationSummaryServerExample>();

        Assert.Equal(["That name is taken.", "New trails close for the winter on December 1."],
            cut.Tb().ValidationSummary().Messages);
    }

    [Fact]
    public void Render_ShouldThrow_WhenOutsideAForm()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbValidationSummary>());
    }

    // A booking that's invalid as a whole, with a message that names no member.
    private sealed class Booking : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            [new("Pick at least one trail.")];
    }
}
