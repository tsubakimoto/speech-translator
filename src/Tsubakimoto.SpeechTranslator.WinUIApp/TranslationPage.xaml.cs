using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tsubakimoto.SpeechTranslator.WinUIApp.Core;
namespace Tsubakimoto.SpeechTranslator.WinUIApp;
/// <summary>Translation controls and virtualized unbounded session logs.</summary>
public sealed partial class TranslationPage : Page
{
    /// <summary>The application's state machine.</summary>
    public MainViewModel ViewModel { get; }
    private bool _isSynchronizing;
    /// <summary>Constructs the page with the committed session model.</summary>
    public TranslationPage(MainViewModel viewModel)
    {
        ViewModel = viewModel; _isSynchronizing = true; InitializeComponent();
        Loaded += (_, _) => _isSynchronizing = false;
        ViewModel.Translations.CollectionChanged += ScrollResults;
        ViewModel.Activity.CollectionChanged += ScrollActivity;
        Unloaded += (_, _) => { ViewModel.Translations.CollectionChanged -= ScrollResults; ViewModel.Activity.CollectionChanged -= ScrollActivity; };
    }
    private void ScrollResults(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => DispatcherQueue.TryEnqueue(() => { if (ViewModel.Translations.Count > 0) Results.ScrollIntoView(ViewModel.Translations[^1]); });
    private void ScrollActivity(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => DispatcherQueue.TryEnqueue(() => { if (ViewModel.Activity.Count > 0) Activity.ScrollIntoView(ViewModel.Activity[^1]); });
    private async void LanguageChangedAsync(object sender, SelectionChangedEventArgs e)
    {
        if (_isSynchronizing || !ViewModel.CanEdit || SourceLanguage?.SelectedValue is not string source || TargetLanguage?.SelectedValue is not string target) return;
        if (source == ViewModel.Source && target == ViewModel.Target) return;
        _isSynchronizing = true;
        try { await ViewModel.SavePreferencesAsync(source, target, ViewModel.CommittedSettings.Folder); }
        finally { SourceLanguage.SelectedValue = ViewModel.Source; TargetLanguage.SelectedValue = ViewModel.Target; _isSynchronizing = false; }
    }
    private async void SelectFolderAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Microsoft.Windows.Storage.Pickers.FolderPicker(App.Window.AppWindow.Id);
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null) await ViewModel.SavePreferencesAsync(ViewModel.Source, ViewModel.Target, folder.Path);
        }
        catch { ViewModel.SettingsMessage = "フォルダーを選択できませんでした。既存の選択は保持されています。"; }
    }
    private async void StartAsync(object sender, RoutedEventArgs e) => await ViewModel.StartAsync();
    private async void StopAsync(object sender, RoutedEventArgs e) => await ViewModel.StopAsync();
}
