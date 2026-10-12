namespace SpeechTranslator.WinUI.Tests;

internal sealed class ControlledSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _work = new();

    /// <summary>Queues continuations for explicit deterministic execution by the fixture.</summary>
    public override void Post(SendOrPostCallback d, object? state) => _work.Enqueue((d, state));

    internal void Drain()
    {
        var remaining = 1000;
        while (_work.TryDequeue(out var item))
        {
            Assert.True(remaining-- > 0, "Continuation loop exceeded the bounded fixture budget.");
            item.Callback(item.State);
        }
    }
}
