using SpeechTranslatorDesktop.Commands;
using SpeechTranslatorDesktop.Services;

namespace SpeechTranslator.Desktop.Tests;

public class AsyncRelayCommandTests
{
    [Fact]
    public void RaiseCanExecuteChanged_UsesDispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        var command = new AsyncRelayCommand(() => Task.CompletedTask, dispatcher: dispatcher);
        var raisedCount = 0;
        command.CanExecuteChanged += (_, _) => raisedCount++;

        command.RaiseCanExecuteChanged();

        dispatcher.InvokeCount.Should().Be(1);
        raisedCount.Should().Be(1);
    }

    private sealed class RecordingDispatcher : IUiDispatcher
    {
        public int InvokeCount { get; private set; }

        public void Invoke(Action action)
        {
            InvokeCount++;
            action();
        }
    }
}
