using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Testing;

/// <summary>Checks a component of your own, such as a field that wraps a Trailblazor one, against the rules
/// Trailblazor's own components keep.</summary>
public static class TbContract
{
    /// <summary>Renders <typeparamref name="TComponent"/> in bUnit, as it would render on an interactive page, and
    /// checks it against the rules every Trailblazor component keeps:
    /// <list type="bullet">
    /// <item>A value its parent sets is never raised back through its <c>Changed</c> callback.</item>
    /// <item>If it has a <c>ValueChanged</c> callback, it never changes its value while <c>Disabled</c> or
    /// <c>ReadOnly</c> is true, however its controls are used.</item>
    /// <item>Once disposed, it leaves no handler on Trailblazor's services or the navigation manager and no reference
    /// the browser can still call, and as time passes it neither calls its parent nor raises an error.</item>
    /// <item>Each of its parts takes its events again after the component renders again, so a test can use an element
    /// it found before.</item>
    /// </list>
    /// Each rule renders the component afresh. Throws a <see cref="TbContractException"/> listing every failure,
    /// including any bound type that needs a <see cref="TbContractSetup.Sample{T}"/>.</summary>
    public static async Task VerifyAsync<TComponent>(Action<TbContractSetup>? setup = null)
        where TComponent : IComponent
    {
        var given = new TbContractSetup();
        setup?.Invoke(given);
        var component = typeof(TComponent);
        var failures = new List<string>();

        foreach (var parameter in ContractRules.BoundParameters(component))
        {
            var type = component.GetProperty(parameter)!.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if ((given.SampleFor(type) ?? ContractRules.Samples(type)) is not { } samples)
            {
                failures.Add($"Call {nameof(TbContractSetup.Sample)} with two {ContractRules.NameOf(type)} values, so "
                    + $"{parameter} can be set from the parent.");
                continue;
            }

            await using var context = given.CreateContext(new FakeTimeProvider());
            failures.AddRange(ContractRules.CheckEcho(context, component, parameter, new(given.Values), samples.First,
                samples.Second));
        }

        if (ContractRules.BoundParameters(component).Contains("Value"))
        {
            foreach (var state in new[] { "Disabled", "ReadOnly" }.Where(state =>
                         component.GetProperty(state) is { } property && property.PropertyType == typeof(bool)
                         && property.IsDefined(typeof(ParameterAttribute), inherit: true)))
            {
                await using var context = given.CreateContext(new FakeTimeProvider());
                failures.AddRange(
                    await ContractRules.CheckTurnedOffAsync(context, component, state, new(given.Values)));
            }
        }

        var time = new FakeTimeProvider();
        await using (var context = given.CreateContext(time))
        {
            var calls = new List<object?>();
            var values = new Dictionary<string, object>(given.Values);
            ContractRules.AddCallbacks(component, values, calls, _ => false);
            failures.AddRange(await ContractRules.CheckDisposeAsync(context, time, parameters => parameters
                .Add(host => host.Type, component).Add(host => host.Parameters, values), component.Name, calls));
        }

        await using (var context = given.CreateContext(new FakeTimeProvider()))
        {
            var cut = context.Render<DynamicComponent>(parameters => parameters.Add(host => host.Type, component)
                .Add(host => host.Parameters, new Dictionary<string, object>(given.Values)));
            failures.AddRange(await ContractRules.CheckHandlersAsync(cut));
        }

        if (failures.Count > 0)
        {
            throw new TbContractException(failures);
        }
    }
}
