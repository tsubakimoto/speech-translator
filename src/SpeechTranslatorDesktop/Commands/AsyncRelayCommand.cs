using System.Windows.Input;
using SpeechTranslatorDesktop.Services;

namespace SpeechTranslatorDesktop.Commands;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly IUiDispatcher? _dispatcher;
    private readonly Action<Exception>? _onException;
    private bool _isExecuting;

    public AsyncRelayCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null,
        IUiDispatcher? dispatcher = null,
        Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _dispatcher = dispatcher;
        _onException = onException;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            if (_onException is null)
            {
                throw;
            }

            _onException(ex);
        }
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        if (_dispatcher is null)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _dispatcher.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}
