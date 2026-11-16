using System.Reflection;
using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests;

// A driver's refusal or failed lookup faults the task it returns, so a test that holds the task, or passes it to
// Task.WhenAll, sees the failure where it awaits.
public class DriverTaskTests : BunitContext
{
    // Drivers that wait for their component to go finish at once when it isn't there.
    private static readonly HashSet<string> DoneWhenGone = ["TbDialogDriver.WaitForClosedAsync"];

    public DriverTaskTests() => this.AddTrailblazor();

    [Fact]
    public async Task Drivers_ShouldFaultTheirTasksInsteadOfThrowing_WhenTheirComponentIsntThere()
    {
        var root = Render(builder => builder.AddMarkupContent(0, "<p>No components here.</p>")).Tb();
        var drivers = new Queue<object>(DriversFrom(root));
        var seen = new HashSet<Type>();
        var failures = new List<string>();

        while (drivers.TryDequeue(out var driver))
        {
            if (!seen.Add(driver.GetType()))
            {
                continue;
            }

            var tasks = Members(driver.GetType()).Where(method => typeof(Task).IsAssignableFrom(method.ReturnType));
            foreach (var method in tasks)
            {
                var name = $"{driver.GetType().Name}.{method.Name}";
                Task task;
                try
                {
                    task = (Task)method.Invoke(driver, Arguments(method))!;
                }
                catch (TargetInvocationException error)
                {
                    failures.Add($"{name} threw {error.InnerException!.GetType().Name} instead of faulting its task.");
                    continue;
                }

                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
                    if (!DoneWhenGone.Contains(name))
                    {
                        failures.Add($"{name} finished without its component.");
                    }
                }
                catch (TimeoutException)
                {
                    failures.Add($"{name} didn't finish without its component.");
                }
                catch (Exception)
                {
                    // Faulted, as it should.
                }
            }

            foreach (var found in DriversFrom(driver))
            {
                drivers.Enqueue(found);
            }
        }

        Assert.Empty(typeof(TrailblazorMarkup).Assembly.GetExportedTypes()
            .Where(type => IsDriver(type) && !type.IsAbstract && !seen.Contains(type)).Select(type => type.Name));
        Assert.Empty(failures);
    }

    // Drivers a driver's lookups return, such as a tree's branch or a context menu on a key, from stand-in arguments.
    private static IEnumerable<object> DriversFrom(object driver)
    {
        foreach (var method in Members(driver.GetType()).Where(method => IsDriver(method.ReturnType)))
        {
            object? found;
            try
            {
                found = method.Invoke(driver, Arguments(method));
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (found is not null)
            {
                yield return found;
            }
        }

        // Drivers can also come from a property, such as TrailblazorMarkup.Toasts.
        foreach (var property in driver.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => IsDriver(property.PropertyType)))
        {
            yield return property.GetValue(driver)!;
        }
    }

    private static IEnumerable<MethodInfo> Members(Type type) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(method => method.DeclaringType!.Assembly == typeof(TrailblazorMarkup).Assembly && !method.IsSpecialName);

    // Scoped markups a driver returns, such as a tab's panel, lead to the same driver types, so they aren't followed.
    private static bool IsDriver(Type type) => type.Assembly == typeof(TrailblazorMarkup).Assembly
        && type.Name.EndsWith("Driver", StringComparison.Ordinal);

    private static object?[] Arguments(MethodInfo method) => [.. method.GetParameters().Select(parameter =>
        parameter.ParameterType == typeof(string) ? "Missing"
        : parameter.ParameterType == typeof(string[]) ? new[] { "Missing" }
        : parameter.ParameterType.IsArray ? Array.CreateInstance(parameter.ParameterType.GetElementType()!, 0)
        : parameter.ParameterType == typeof(int) ? 1
        : Nullable.GetUnderlyingType(parameter.ParameterType) is not null || !parameter.ParameterType.IsValueType
            ? null
            : Activator.CreateInstance(parameter.ParameterType))];
}
