using System.Linq.Expressions;

namespace Trailblazor.Tests.Field;

public class FieldNamesTests
{
    private Order Model { get; } = new();

    [Fact]
    public void From_ShouldNameTheMemberChain_WhenGivenAPropertyOfTheComponent()
    {
        Expression<Func<string?>> expression = () => Model.Address.City;

        Assert.Equal("Model.Address.City", FieldNames.From(expression));
    }

    [Fact]
    public void From_ShouldStartAtTheCapturedVariable_WhenGivenALocal()
    {
        var order = new Order();
        Expression<Func<string?>> expression = () => order.Reference;

        Assert.Equal("order.Reference", FieldNames.From(expression));
    }

    [Fact]
    public void From_ShouldReturnNull_WhenGivenAnIndexer()
    {
        var orders = new List<Order> { new() };
        Expression<Func<string?>> expression = () => orders[0].Reference;

        Assert.Null(FieldNames.From(expression));
    }

    private sealed class Order
    {
        public string? Reference { get; set; }

        public Address Address { get; } = new();
    }

    private sealed class Address
    {
        public string? City { get; set; }
    }
}
