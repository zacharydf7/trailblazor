using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests.Contract;

// A script's call arrives outside Blazor's events, so what it throws would go back to the script and show only in the
// browser's console. Every object the library hands a script must hand a failure to its owner instead.
public partial class ScriptListenerContractTests
{
    private const string Thrown = "Thrown by the app's callback.";

    // The browser service's listener belongs to no component, so it logs a failure instead. A dismissable layer is
    // made by its own CreateAsync, which passes on the handler its caller gives.
    private static readonly HashSet<string> Exempt = ["BrowserService.cs", "DismissableLayer.cs"];

    private static readonly Type[] Called = [.. new[] { typeof(TbButton).Assembly, typeof(ScriptListener).Assembly }
        .SelectMany(assembly => assembly.GetTypes()).Where(type => Calls(type).Any())
        .OrderBy(type => type.Name, StringComparer.Ordinal)];

    public static TheoryData<Type, string> Entries => new(Called.Where(type => type.IsSubclassOf(typeof(ScriptListener)))
        .SelectMany(type => Calls(type).Select(call => (type, call.Name))));

    [Fact]
    public void Type_ShouldBeAScriptListener_WhenAScriptCallsIt()
    {
        var others = Called.Where(type => !type.IsSubclassOf(typeof(ScriptListener))).Select(type => type.Name);

        // The dialog's frame is a component, so it runs its two calls through ScriptListener.RunAsync itself.
        // DialogServiceTests checks the one that reaches the app's code, Dismiss.
        Assert.Equal(["DialogFrame"], others);
    }

    [Theory, MemberData(nameof(Entries))]
    public async Task Call_ShouldHandTheFailureToItsOwner_WhenWhatItCallsThrows(Type listener, string call)
    {
        var failures = new List<Exception>();
        var made = Make(listener, failures);
        var method = made.GetType().GetMethod(call)!;

        var answer = method.Invoke(made, [.. method.GetParameters().Select(parameter => Sample(parameter.ParameterType))]);

        await Assert.IsAssignableFrom<Task>(answer);
        Assert.Equal(Thrown, Assert.Single(failures).Message);
    }

    [Fact]
    public void Source_ShouldHandEachListenersFailuresToItsComponent_WhenItMakesOne()
    {
        var listeners = Called.Where(type => type.IsSubclassOf(typeof(ScriptListener)))
            .Select(type => type.Name.Split('`')[0]).ToHashSet();
        var built = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var sources = new[] { "Trailblazor", "Trailblazor.Primitives" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(Repository.Root, "src", project), "*.*",
                SearchOption.AllDirectories))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor"
                && !file.Contains(built, StringComparison.Ordinal) && !Exempt.Contains(Path.GetFileName(file)))
            .Select(file => (Name: Path.GetFileName(file), Text: File.ReadAllText(file))).ToList();
        var made = sources.SelectMany(source => Made().Matches(source.Text)
                .Where(site => listeners.Contains(site.Groups["type"].Value))
                .Select(site => (source.Name, Type: site.Groups["type"].Value, First: site.Groups["first"].Value)))
            .ToList();
        var run = sources.SelectMany(source => RunCalls().Matches(source.Text)
            .Select(call => (source.Name, Arguments: call.Groups["arguments"].Value))).ToList();

        Assert.Empty(made.Where(site => site.First != "DispatchExceptionAsync")
            .Select(site => $"{site.Name}: new {site.Type}({site.First}"));
        Assert.Empty(run
            .Where(call => !Regex.IsMatch(call.Arguments, @",\s*DispatchExceptionAsync\s*$"))
            .Select(call => $"{call.Name}: {call.Arguments}"));
        // Each listener is made with its type named somewhere, so none is made only where this can't read it, as with
        // a target-typed new.
        Assert.Empty(listeners.Except(made.Select(site => site.Type)).Except([nameof(BrowserListener<>), nameof(DismissableLayer)]));
        Assert.Contains(run, call => call.Name == "TbOverlay.razor");
    }

    // A listener made with its type named, or with a target-typed new inside Create<T>, and its first argument.
    [GeneratedRegex(@"(?:\bnew (?<type>[A-Z]\w*)(?:<[^>()]*>)?|\bCreate<(?<type>[A-Z]\w*)(?:<[^>()]*>)?>\(\s*new)"
        + @"\(\s*(?<first>[\w.]+)")]
    private static partial Regex Made();

    // A call that runs a script's work through ScriptListener, or makes a dismissable layer, with all its arguments,
    // however they nest.
    [GeneratedRegex(@"(?:ScriptListener\.RunAsync|DismissableLayer\.CreateAsync)\("
        + @"(?<arguments>(?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!)))\)")]
    private static partial Regex RunCalls();

    private static IEnumerable<MethodInfo> Calls(Type type) => type
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        .Where(method => method.GetCustomAttribute<JSInvokableAttribute>() is not null);

    // The listener with every callback throwing, and with its failures collected.
    private static object Make(Type listener, List<Exception> failures)
    {
        var closed = listener.IsGenericTypeDefinition ? listener.MakeGenericType(typeof(int)) : listener;
        var constructor = closed.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => parameter.ParameterType switch
        {
            var type when type == typeof(Func<Exception, Task>) => (Func<Exception, Task>)(failure =>
            {
                failures.Add(failure);
                return Task.CompletedTask;
            }),
            var type when type == typeof(EventCallback<DismissReason>) =>
                EventCallback.Factory.Create<DismissReason>(failures, _ => throw new InvalidOperationException(Thrown)),
            var type when typeof(Delegate).IsAssignableFrom(type) => Throwing(type),
            var type => Sample(type),
        })]);
    }

    private static Delegate Throwing(Type callback)
    {
        var invoke = callback.GetMethod(nameof(Action.Invoke))!;
        return Expression.Lambda(callback,
            Expression.Throw(Expression.Constant(new InvalidOperationException(Thrown)), invoke.ReturnType),
            invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType))).Compile();
    }

    private static object? Sample(Type type) => type == typeof(string) ? "sample"
        : type.IsArray ? Array.CreateInstance(type.GetElementType()!, 0)
        : type.IsValueType ? Activator.CreateInstance(type) : null;
}
