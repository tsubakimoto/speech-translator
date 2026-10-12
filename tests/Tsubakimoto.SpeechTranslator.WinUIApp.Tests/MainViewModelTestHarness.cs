using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

internal sealed class MainViewModelTestHarness : IDisposable
{
    private readonly SynchronizationContext? _previous = SynchronizationContext.Current;
    private readonly Queue<Action> _dispatch = new();
    internal ControlledSynchronizationContext Context { get; } = new();
    internal ISettingsStore Store { get; } = Substitute.For<ISettingsStore>();
    internal ISpeechSession Speech { get; } = Substitute.For<ISpeechSession>();
    internal IRecordingWriter Recording { get; } = Substitute.For<IRecordingWriter>();
    internal TaskCompletionSource Delay { get; } = new();
    internal List<TimeSpan> Delays { get; } = [];
    internal MainViewModel Model { get; }
    internal bool QueueDispatch { get; set; }

    internal MainViewModelTestHarness(Settings? saved = null, IRecordingWriter? recording = null)
    {
        SynchronizationContext.SetSynchronizationContext(Context);
        Store.LoadAsync().Returns(Task.FromResult(saved));
        Store.SaveAsync(Arg.Any<Settings>()).Returns(Task.CompletedTask);
        Store.RecoverAsync(Arg.Any<Settings>()).Returns(Task.FromResult("backup.db"));
        Speech.StartAsync(Arg.Any<Settings>()).Returns(Task.CompletedTask);
        Speech.StopAsync().Returns(Task.CompletedTask);
        Recording.Validate(Arg.Any<string?>(), Arg.Any<string>()).Returns((string?)null);
        Model = new(Store, Speech, recording ?? Recording, action =>
        {
            if (QueueDispatch) _dispatch.Enqueue(action); else action();
        }, duration => { Delays.Add(duration); return Delay.Task; });
    }

    internal Task InitializeAsync() => Model.InitializeAsync();
    internal void Pump() => Context.Drain();
    internal void DrainDispatcher()
    {
        while (_dispatch.TryDequeue(out var action)) action();
        Pump();
    }
    internal void Translate(string source = "source", string target = "訳") => Speech.Translated += Raise.Event<Action<Translation>>(new Translation(source, target));
    internal void End(string message = "ended") => Speech.Ended += Raise.Event<Action<string>>(message);
    internal void Status(string message) => Speech.Status += Raise.Event<Action<string>>(message);
    internal static void Completed(Task task)
    {
        Assert.True(task.IsCompleted, "Controlled gates were released but operation did not complete (possible missing cleanup or self-await deadlock).");
        task.GetAwaiter().GetResult();
    }
    internal static string[] Messages(MainViewModel model) => model.Activity.Select(line => line[(line.IndexOf(' ') + 1)..]).ToArray();

    /// <summary>Releases the owned delay and restores the caller synchronization context.</summary>
    public void Dispose()
    {
        Delay.TrySetResult();
        SynchronizationContext.SetSynchronizationContext(_previous);
    }
}
