using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Contract;

// A script's call arrives outside Blazor's events, so what it throws would go back to the script and show only in the
// browser's console. Every object the library hands a script must hand a failure to its owner instead.
public class ScriptListenerContractTests
{
    private const string Thrown = "Thrown by the app's callback.";

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
