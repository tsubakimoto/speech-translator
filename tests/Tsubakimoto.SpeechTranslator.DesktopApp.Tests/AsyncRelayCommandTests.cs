using Tsubakimoto.SpeechTranslator.DesktopApp.Commands;
using Tsubakimoto.SpeechTranslator.DesktopApp.Services;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class AsyncRelayCommandTests
{
    [Fact]
    public void RaiseCanExecuteChanged_UsesDispatcher()
    {
        // Arrange
        var dispatcher = new RecordingDispatcher();
        var command = new AsyncRelayCommand(() => Task.CompletedTask, dispatcher: dispatcher);
        var raisedCount = 0;
        command.CanExecuteChanged += (_, _) => raisedCount++;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        Assert.Equal(1, dispatcher.InvokeCount);
        Assert.Equal(1, raisedCount);
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