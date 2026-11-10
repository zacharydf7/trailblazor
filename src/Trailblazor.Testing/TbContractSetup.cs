using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Testing;

/// <summary>What <see cref="TbContract.VerifyAsync{TComponent}"/> renders the component with.</summary>
public sealed class TbContractSetup
{
    private readonly List<Action<IServiceCollection>> _registrations = [];
    private readonly List<Action<BunitContext>> _contexts = [];

    internal TbContractSetup()
    {
    }

    internal Dictionary<string, object> Values { get; } = [];

    internal Dictionary<Type, (object First, object Second)> Samples { get; } = [];

    /// <summary>Gives every render of the component this parameter, such as one it requires.</summary>
    public TbContractSetup Parameter(string name, object value)
    {
        Values[name] = value;
        return this;
    }

    /// <summary>Registers services the component needs besides Trailblazor's own, which are already added as
    /// <see cref="TrailblazorBunitExtensions.AddTrailblazor"/> adds them.</summary>
    public TbContractSetup Services(Action<IServiceCollection> register)
    {
        _registrations.Add(register);
        return this;
    }

    /// <summary>Configures each test context after Trailblazor's services are added, for example to set up the
    /// JavaScript calls the component makes itself through <c>JSInterop</c>.</summary>
    public TbContractSetup Context(Action<BunitContext> configure)
    {
        _contexts.Add(configure);
        return this;
    }

    /// <summary>Two different values of a type the component binds, which the rules set from the parent.
    /// <see cref="string"/>, <see cref="bool"/>, the numeric types, <see cref="DateOnly"/>, <see cref="TimeOnly"/>,
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/> and Trailblazor's own value types
    /// (date and slider ranges, and the grid's state, sorts and filters) need none.</summary>
    public TbContractSetup Sample<T>(T first, T second) where T : notnull
    {
        Samples[typeof(T)] = (first, second);
        return this;
    }

    // The samples given for a bound type: those of the type itself, or else of one it accepts, such as an array for a
    // list.
    internal (object First, object Second)? SampleFor(Type type) =>
        Samples.TryGetValue(type, out var exact) ? exact
        : Samples.Where(pair => type.IsAssignableFrom(pair.Key)).Select(pair => pair.Value)
            .Cast<(object, object)?>().FirstOrDefault();

    // A context of its own for each rule, with a clock the rules move on themselves.
    internal BunitContext CreateContext(FakeTimeProvider time)
    {
        var context = new BunitContext();
        context.Services.AddSingleton<TimeProvider>(time);
        foreach (var register in _registrations)
        {
            register(context.Services);
        }

        context.AddTrailblazor();
        foreach (var configure in _contexts)
        {
            configure(context);
        }

        return context;
    }
}
