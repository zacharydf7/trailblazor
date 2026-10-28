using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor.Tests.Components;

public class TbTextFieldTests : BunitContext
{
    [Fact]
    public void Render_ShouldLinkTheLabelAndHelperToTheInput_WhenBothAreSet()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name")
            .Add(field => field.HelperText, "As on your badge")
            .Add(field => field.Required, true));

        var input = cut.Find("[data-part=input]");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(cut.Find("[data-part=helper]").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("true", input.GetAttribute("aria-required"));
        Assert.Null(input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task Change_ShouldAwaitValueChanged_WhenTheTextIsCommitted()
    {
        var handler = new TaskCompletionSource();
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueChanged, (string _) => handler.Task));

        var change = cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Ada" });
        Assert.False(change.IsCompleted);
        handler.SetResult();
        await change;
    }

    [Fact]
    public void Render_ShouldNotRaiseValueChanged_WhenTheParentSetsTheValue()
    {
        var raised = false;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, "Ada")
            .Add(field => field.ValueChanged, (string _) => raised = true));

        cut.Render(parameters => parameters.Add(field => field.Value, "Grace"));

        Assert.Equal("Grace", cut.Find("input").GetAttribute("value"));
        Assert.False(raised);
    }

    [Fact]
    public void Input_ShouldNotBeWired_WhenUpdateOnInputIsOff()
    {
        var cut = Render<TbTextField<string>>();

        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Input("A"));
    }

    [Fact]
    public void Input_ShouldUpdateTheValue_WhenUpdateOnInputIsOn()
    {
        string? value = null;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.UpdateOnInput, true)
            .Add(field => field.ValueChanged, (string text) => value = text));

        cut.Find("input").Input("A");

        Assert.Equal("A", value);
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Change("AB"));
    }

    [Fact]
    public void Change_ShouldKeepTheValueAndShowAnError_WhenTheTextDoesNotParse()
    {
        var raised = false;
        var cut = Render<TbTextField<int>>(parameters => parameters
            .Add(field => field.Label, "Age")
            .Add(field => field.Value, 30)
            .Add(field => field.ValueChanged, (int _) => raised = true));

        cut.Find("input").Change("thirty");

        var input = cut.Find("input");
        Assert.Equal("30", input.GetAttribute("value"));
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[data-part=error]").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("Age isn't valid.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.False(raised);
    }

    [Fact]
    public void Change_ShouldClearTheParseError_WhenValidTextFollows()
    {
        var cut = Render<TbTextField<int>>(parameters => parameters.Add(field => field.Label, "Age"));
        cut.Find("input").Change("thirty");

        cut.Find("input").Change("31");

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Equal("31", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldShowValidationMessages_WhenValidationFails()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Validation, (string? text) => text?.Length < 3 ? ["Too short."] : []));

        cut.Find("input").Change("Al");

        Assert.Equal("Too short.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Submit_ShouldBlockTheFormAndShowTheMessage_WhenValidationFailsInAnEditForm()
    {
        var model = new Person();
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model)));

        cut.Find("form").Submit();

        Assert.False(submitted);
        Assert.Equal("Enter a name.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldNameTheInputAfterTheBoundProperty_WhenValueExpressionIsSet()
    {
        var model = new Person();
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueExpression, () => model.Name!));

        Assert.Equal("model.Name", cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void Submit_ShouldGoThrough_WhenTheFieldIsFixedAfterAFailedSubmit()
    {
        var model = new Person();
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model)));
        cut.Find("form").Submit();

        cut.Find("input").Change("Ada");
        cut.Find("form").Submit();

        Assert.True(submitted);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Submit_ShouldGoThrough_WhenAFieldWithAMessageIsReboundToAValidProperty()
    {
        var first = new Person();
        var second = new Person { Name = "Grace" };
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, first)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(first)));
        cut.Find("form").Submit();

        cut.Render(parameters => parameters.Add(form => form.ChildContent, (EditContext _) => NameField(second)));
        cut.Find("form").Submit();

        Assert.True(submitted);
    }

    [Fact]
    public async Task Change_ShouldNotValidate_WhenTheFieldIsDisposedWhileValueChangedRuns()
    {
        var handler = new TaskCompletionSource();
        var validations = 0;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueChanged, (string _) => handler.Task)
            .Add(field => field.Validation, (string? _) =>
            {
                validations++;
                return [];
            }));
        var change = cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Ada" });

        await DisposeComponentsAsync();
        handler.SetResult();
        await change;

        Assert.Equal(0, validations);
    }

    [Fact]
    public void Render_ShouldKeepTheConsumerNameAndDescription_WhenTheFieldHasNoneOfItsOwn()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.InputAttributes, new Dictionary<string, object>
            {
                ["name"] = "lines[0].qty", ["aria-describedby"] = "qty-hint",
            })
            .Add(field => field.HelperText, "Whole units"));

        var input = cut.Find("input");
        Assert.Equal("lines[0].qty", input.GetAttribute("name"));
        Assert.Equal($"{cut.Find("[data-part=helper]").Id} qty-hint", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Change_ShouldUpdateTheElement_WhenBoundToAListElement()
    {
        var tags = new List<string> { "red" };
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, tags[0])
            .Add(field => field.ValueChanged, (string tag) => tags[0] = tag)
            .Add(field => field.ValueExpression, () => tags[0]));

        cut.Find("input").Change("blue");

        Assert.Equal("blue", tags[0]);
        Assert.Null(cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void Render_ShouldNotThrow_WhenGivenAnExpressionFormsCannotIdentifyOutsideAForm()
    {
        var tags = new List<string> { "red" };
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, tags.First())
            .Add(field => field.ValueExpression, () => tags.First()));

        Assert.Equal("red", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldRenderATextArea_WhenLinesIsMoreThanOne()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Lines, 4));

        Assert.Equal("4", cut.Find("textarea").GetAttribute("rows"));
    }

    [Fact]
    public void Render_ShouldPutInputAttributesOnTheInput_WhenTheyArePassed()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.InputAttributes, new Dictionary<string, object> { ["autocomplete"] = "name" }));

        Assert.Equal("name", cut.Find("input").GetAttribute("autocomplete"));
    }

    // A name field bound to the model, which requires a name.
    private static RenderFragment NameField(Person model) => builder =>
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddAttribute(1, "Value", model.Name);
        builder.AddAttribute(2, "ValueChanged",
            EventCallback.Factory.Create<string>(new object(), (string name) => model.Name = name));
        builder.AddAttribute(3, "ValueExpression", (System.Linq.Expressions.Expression<Func<string>>)(() => model.Name!));
        builder.AddAttribute(4, "Validation", (Func<string?, IEnumerable<string>>)(name =>
            string.IsNullOrEmpty(name) ? ["Enter a name."] : []));
        builder.CloseComponent();
    };

    private sealed class Person
    {
        public string? Name { get; set; }
    }
}
