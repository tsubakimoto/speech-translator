using Tsubakimoto.SpeechTranslator.DesktopApp.Services;
using Tsubakimoto.SpeechTranslator.Shared;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class DesktopTranslationControllerTests
{
    [Fact]
    public async Task StopAsync_WhenSessionStopFails_KeepsControllerRunningAsync()
    {
        // Arrange
        var session = new FakeTranslationSession();
        session.EnqueueStopBehavior(() => throw new InvalidOperationException("stop failed"));
        session.EnqueueStopBehavior(() =>
        {
            session.Complete();
            return Task.CompletedTask;
        });
        var controller = CreateController(session);

        // Act
        await controller.StartAsync(new SpeechCredentials("japaneast", "test-key"), "en-US", "ja-JP", new NoOpTranslationRecognizerWorker());
        // Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => controller.StopAsync());
        Assert.Equal("stop failed", exception.Message, ignoreCase: true);

        Assert.True(controller.IsRunning);
        Assert.Equal(0, session.DisposeCallCount);

        await controller.StopAsync();

        Assert.False(controller.IsRunning);
        Assert.Equal(1, session.DisposeCallCount);
    }

    [Fact]
    public async Task StartAsync_AfterStopFailure_ThrowsWhileOriginalSessionIsTrackedAsync()
    {
        // Arrange
        var session = new FakeTranslationSession();
        session.EnqueueStopBehavior(() => throw new InvalidOperationException("stop failed"));
        session.EnqueueStopBehavior(() =>
        {
            session.Complete();
            return Task.CompletedTask;
        });
        var controller = CreateController(session);

        // Act
        await controller.StartAsync(new SpeechCredentials("japaneast", "test-key"), "en-US", "ja-JP", new NoOpTranslationRecognizerWorker());
        // Assert
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => controller.StopAsync());

        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => controller.StartAsync(new SpeechCredentials("japaneast", "test-key"), "en-US", "ja-JP", new NoOpTranslationRecognizerWorker()));
        Assert.Equal("Translation is already running.", exception.Message, ignoreCase: true);

        await controller.StopAsync();
        Assert.False(controller.IsRunning);
    }

    [Fact]
    public async Task StopAsync_WhenDisposeFails_KeepsControllerRunningUntilRetrySucceedsAsync()
    {
        // Arrange
        var session = new FakeTranslationSession();
        session.EnqueueStopBehavior(() =>
        {
            session.Complete();
            return Task.CompletedTask;
        });
        session.EnqueueStopBehavior(() => Task.CompletedTask);
        session.EnqueueDisposeBehavior(() => ValueTask.FromException(new InvalidOperationException("dispose failed")));
        session.EnqueueDisposeBehavior(() => ValueTask.CompletedTask);
        var controller = CreateController(session);

        // Act
        await controller.StartAsync(new SpeechCredentials("japaneast", "test-key"), "en-US", "ja-JP", new NoOpTranslationRecognizerWorker());
        // Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => controller.StopAsync());
        Assert.Equal("dispose failed", exception.Message, ignoreCase: true);

        Assert.True(controller.IsRunning);
        Assert.Equal(1, session.DisposeCallCount);

        await controller.StopAsync();

        Assert.False(controller.IsRunning);
        Assert.Equal(2, session.DisposeCallCount);
    }

    private static DesktopTranslationController CreateController(ITranslationSession session)
    {
        return new DesktopTranslationController((credentials, sourceLanguage, targetLanguage, worker, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(session);
        });
    }

    private sealed class FakeTranslationSession : ITranslationSession
    {
        private readonly Queue<Func<Task>> _stopBehaviors = new();
        private readonly Queue<Func<ValueTask>> _disposeBehaviors = new();
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completion => _completion.Task;

        public bool IsRunning { get; private set; } = true;

        public int DisposeCallCount { get; private set; }

        public int StopCallCount { get; private set; }

        public void EnqueueDisposeBehavior(Func<ValueTask> behavior) => _disposeBehaviors.Enqueue(behavior);

        public void EnqueueStopBehavior(Func<Task> behavior) => _stopBehaviors.Enqueue(behavior);

        public void Complete()
        {
            IsRunning = false;
            _completion.TrySetResult();
        }

        public Task StopAsync()
        {
            StopCallCount++;
            return _stopBehaviors.Count > 0 ? _stopBehaviors.Dequeue().Invoke() : Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCallCount++;
            IsRunning = false;
            return _disposeBehaviors.Count > 0 ? _disposeBehaviors.Dequeue().Invoke() : ValueTask.CompletedTask;
        }
    }

    private sealed class NoOpTranslationRecognizerWorker : TranslationRecognizerWorkerBase
    {
        public override void OnCanceled(TranslationRecognitionCanceledEventArgs e)
        {
        }

        public override void OnRecognized(TranslationRecognitionEventArgs e)
        {
        }

        public override void OnRecognizing(TranslationRecognitionEventArgs e)
        {
        }

        public override void OnSessionStarted(SessionEventArgs e)
        {
        }

        public override void OnSessionStopped(SessionEventArgs e)
        {
        }

        public override void OnSpeechEndDetected(RecognitionEventArgs e)
        {
        }

        public override void OnSpeechStartDetected(RecognitionEventArgs e)
        {
        }
    }
}