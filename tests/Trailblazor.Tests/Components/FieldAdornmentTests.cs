using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Tests.Contract;

namespace Trailblazor.Tests.Components;

// Start and End on every boxed field whose box has room for them.
public class FieldAdornmentTests : BunitContext
{
    // A code field's boxes hold one character each, and an editor's box is taken up by its toolbar and its text. The
    // mention input has room, and gets them in #622's follow-up.
    private static readonly Type[] Without =
        [typeof(TbCodeField), typeof(TbRichTextEditor), typeof(TbMarkdownEditor), typeof(TbMentionInput)];

    public FieldAdornmentTests() => this.AddTrailblazor();

    public static TheoryData<Type, bool> Fields
    {
        get
        {
            var data = new TheoryData<Type, bool>();
            foreach (var boxed in ComponentContractTests.BoxedFields.Select(row => row.Data).Except(Without))
            {
                data.Add(boxed, false);
                data.Add(boxed, true);
            }

            return data;
        }
    }

    [Fact]
    public void BoxedFields_ShouldTakeStartAndEnd_WhenTheirBoxHasRoom()
    {
        var boxed = ComponentContractTests.BoxedFields.Select(row => row.Data);

        var without = boxed.Where(type => type.GetProperty("Start")?.PropertyType != typeof(RenderFragment)
            || type.GetProperty("End")?.PropertyType != typeof(RenderFragment));

        Assert.Equal(Without.Select(type => type.Name).Order(), without.Select(type => type.Name).Order());
    }

    [Theory, MemberData(nameof(Fields))]
    public void Render_ShouldPutTheAppsStartAndEndFirstInTheBoxsSlots_WhenGivenStartAndEnd(Type field, bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        var parameters = ValueContractTests.BaseParameters(field);
        parameters["Start"] = (RenderFragment)(builder => builder.AddMarkupContent(0, "<i class=\"app-start\"></i>"));
        parameters["End"] = (RenderFragment)(builder => builder.AddMarkupContent(0, "<i class=\"app-end\"></i>"));

        var cut = Render<DynamicComponent>(host => host.Add(dynamic => dynamic.Type, field)
            .Add(dynamic => dynamic.Parameters, parameters));

        Assert.Equal("app-start", Assert.Single(cut.FindAll("[data-part=start]")).FirstElementChild?.ClassName);
        Assert.Equal("app-end", Assert.Single(cut.FindAll("[data-part=end]")).FirstElementChild?.ClassName);
    }
}
