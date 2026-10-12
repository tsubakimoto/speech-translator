using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpeechTranslatorWinUI.Core;
using System.ComponentModel;
namespace SpeechTranslatorWinUI;
/// <summary>Explicit credential drafts, commit, discard and confirmed recovery.</summary>
public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    /// <summary>Injected state machine.</summary>
    public MainViewModel ViewModel { get; }
    /// <summary>Allows recovery drafts without allowing other edits during read errors.</summary>
    public bool CanEditDraft => ViewModel.IsInitialized && !ViewModel.IsFrozen && !ViewModel.IsStopPending && !ViewModel.IsPersisting;
    /// <summary>Only explicit read-error recovery is enabled.</summary>
    public bool CanRecover => CanEditDraft && ViewModel.HasRecoveryError;
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>Constructs the settings page.</summary>
    public SettingsPage(MainViewModel viewModel)
    {
        ViewModel = viewModel; InitializeComponent(); ViewModel.PropertyChanged += Changed;
        Unloaded += (_, _) => ViewModel.PropertyChanged -= Changed;
    }
    private void Changed(object? s, PropertyChangedEventArgs e) { PropertyChanged?.Invoke(this, new(nameof(CanEditDraft))); PropertyChanged?.Invoke(this, new(nameof(CanRecover))); }
    private async void SaveAsync(object sender, RoutedEventArgs e) => await ViewModel.SaveSettingsAsync();
    private void Cancel(object sender, RoutedEventArgs e) => ViewModel.CancelSettings();
    private async void RecoverAsync(object sender, RoutedEventArgs e)
    {
        if (await ((MainWindow)App.Window).ConfirmRecoveryAsync()) await ViewModel.RecoverAsync();
    }
}
