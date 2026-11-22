using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

// The base of the objects a component hands its script to call back. A script's call arrives outside Blazor's events,
// so an exception it throws, including one from the app's callback, would go back to the script, where only the
// browser's console shows it. Each call runs through here, which hands the exception to the component's renderer
// instead, as Blazor does for an event handler's.
internal abstract class ScriptListener(Func<Exception, Task> onException)
{
    protected Task Run(Action call) => Run(() =>
    {
        call();
        return Task.CompletedTask;
    });

    protected Task Run(Func<Task> call) => RunAsync(call, onException);

    // For a component that is itself what its script calls back.
    internal static async Task RunAsync(Func<Task> call, Func<Exception, Task> onException)
    {
        try
        {
            await call();
        }
        catch (Exception exception)
        {
            await ReportAsync(exception, onException);
        }
    }

    // The script still gets an answer when the call fails.
    protected async Task<T> Run<T>(Func<Task<T>> call, T otherwise)
    {
        try
        {
            return await call();
        }
        catch (Exception exception)
        {
            await ReportAsync(exception, onException);
            return otherwise;
        }
    }

    // A canceled call ends quietly, as a canceled event handler does, and so does one cut short by a circuit that
    // is gone.
    private static Task ReportAsync(Exception exception, Func<Exception, Task> onException) =>
        exception is OperationCanceledException or JSDisconnectedException ? Task.CompletedTask
            : onException(exception);
}
