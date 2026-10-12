namespace SpeechTranslatorWinUI.Core;

internal enum CloseDecision { Retry, ExitAnyway, Cancel }

/// <summary>Serializes close attempts while retaining the model's single pending stop task.</summary>
internal sealed class CloseCoordinator(MainViewModel model, Func<Task<bool>> confirmDraftAsync, Func<Task<CloseDecision>> decideAsync, Func<TimeSpan, Task> delayAsync)
{
    private bool _isClosing;

    internal async Task<bool> TryCloseAsync()
    {
        if (_isClosing || !model.IsInitialized || model.IsPersisting) return false;
        _isClosing = true;
        try
        {
            if (!await confirmDraftAsync()) return false;
            if (model.IsPersisting) return false;
            while (model.IsFrozen)
            {
                var stop = model.StopAsync();
                if (await Task.WhenAny(stop, delayAsync(TimeSpan.FromSeconds(10))) == stop)
                {
                    await stop;
                    if (!model.IsFrozen) break;
                }
                switch (await decideAsync())
                {
                    case CloseDecision.Cancel: return false;
                    case CloseDecision.ExitAnyway: return true;
                }
            }
            return true;
        }
        finally { _isClosing = false; }
    }
}
