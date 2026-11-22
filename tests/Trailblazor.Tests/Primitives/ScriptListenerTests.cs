using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class ScriptListenerTests
{
    [Fact]
    public async Task Run_ShouldHandTheFailureToItsOwnerAndNotThrow_WhenTheCallThrows()
    {
        var listener = new Listener();
        var failed = new InvalidOperationException("Offline.");

        await listener.Call(() => throw failed);

        Assert.Same(failed, Assert.Single(listener.Failures));
    }

    [Fact]
    public async Task Run_ShouldHandTheFailureToItsOwner_WhenTheCallFailsAfterItWaits()
    {
        var listener = new Listener();

        await listener.Call(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Offline.");
        });

        Assert.Equal("Offline.", Assert.Single(listener.Failures).Message);
    }

    [Fact]
    public async Task Run_ShouldEndQuietly_WhenTheCallIsCanceledOrTheCircuitIsGone()
    {
        var listener = new Listener();

        await listener.Call(() => throw new OperationCanceledException());
        await listener.Call(() => throw new TaskCanceledException());
        await listener.Call(() => throw new JSDisconnectedException("The circuit is gone."));

        Assert.Empty(listener.Failures);
    }

    [Fact]
    public async Task Run_ShouldAnswerTheScriptWithTheFallback_WhenACallThatAnswersThrowsOrIsCanceled()
    {
        var listener = new Listener();

        var failed = await listener.Ask(() => throw new InvalidOperationException("Offline."), otherwise: -1);
        var canceled = await listener.Ask(() => throw new OperationCanceledException(), otherwise: -1);
        var answered = await listener.Ask(() => Task.FromResult(3), otherwise: -1);

        Assert.Equal((-1, -1, 3), (failed, canceled, answered));
        Assert.Equal("Offline.", Assert.Single(listener.Failures).Message);
    }

    [Fact]
    public async Task Run_ShouldHandTheFailureToItsOwner_WhenACallThatReturnsNothingThrows()
    {
        var listener = new Listener();

        await listener.Do(() => throw new InvalidOperationException("Offline."));

        Assert.Equal("Offline.", Assert.Single(listener.Failures).Message);
    }

    private sealed class Listener : ScriptListener
    {
        private readonly List<Exception> _failures;

        public Listener() : this([])
        {
        }

        private Listener(List<Exception> failures) : base(failure =>
        {
            failures.Add(failure);
            return Task.CompletedTask;
        }) => _failures = failures;

        public IReadOnlyList<Exception> Failures => _failures;

        public Task Call(Func<Task> call) => Run(call);

        public Task Do(Action call) => Run(call);

        public Task<int> Ask(Func<Task<int>> call, int otherwise) => Run(call, otherwise);
    }
}
