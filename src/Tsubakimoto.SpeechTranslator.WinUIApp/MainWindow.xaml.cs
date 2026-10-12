using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Tsubakimoto.SpeechTranslator.WinUIApp.Core;
namespace Tsubakimoto.SpeechTranslator.WinUIApp;
/// <summary>Two-page shell with serialized dialogs and stop-before-close lifecycle.</summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CloseCoordinator _close;
    private bool _isClosing;
    private bool _canClose;
    private bool _isNavigating;
    private readonly SemaphoreSlim _dialogGate = new(1, 1);
    /// <summary>Creates the shell around the application model.</summary>
    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel; InitializeComponent(); Title = "Speech Translator";
        _close = new CloseCoordinator(viewModel, ConfirmDraftAsync, DecideCloseAsync, Task.Delay);
        
        AppWindow.Closing += ClosingAsync;
        Root.Loaded += InitializeAsync;
    }
    private async void InitializeAsync(object sender, RoutedEventArgs e)
    {
        Root.Loaded -= InitializeAsync;
        var scale = Root.XamlRoot.RasterizationScale;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(1000 * scale), (int)(850 * scale)));
        await _viewModel.InitializeAsync(new Settings(Environment.GetEnvironmentVariable("SPEECH_REGION") ?? "", Environment.GetEnvironmentVariable("SPEECH_KEY") ?? ""));
        ShowPage();
    }
    private void ShowPage()
    {
        RootFrame.Content = _viewModel.IsSettingsVisible ? new SettingsPage(_viewModel) : new TranslationPage(_viewModel);
        PageButton.Content = _viewModel.IsSettingsVisible ? "翻訳に戻る" : "設定";
    }
    private async void ChangePageAsync(object sender, RoutedEventArgs e)
    {
        if (_isNavigating || _isClosing || !_viewModel.IsInitialized) return;
        _isNavigating = true;
        try { if (_viewModel.IsSettingsVisible && !await ConfirmDraftAsync()) return; _viewModel.IsSettingsVisible = !_viewModel.IsSettingsVisible; ShowPage(); }
        finally { _isNavigating = false; }
    }
    private async Task<ContentDialogResult> DialogAsync(string title, string text, string primary, string secondary, string close)
    {
        await _dialogGate.WaitAsync();
        try
        {
            var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = title, Content = text, PrimaryButtonText = primary, SecondaryButtonText = secondary, CloseButtonText = close, DefaultButton = ContentDialogButton.Close };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(dialog, "DecisionDialog");
            dialog.PrimaryButtonStyle = CreateDialogButtonStyle(dialog.PrimaryButtonStyle, "DecisionPrimaryButton");
            dialog.SecondaryButtonStyle = CreateDialogButtonStyle(dialog.SecondaryButtonStyle, "DecisionSecondaryButton");
            dialog.CloseButtonStyle = CreateDialogButtonStyle(dialog.CloseButtonStyle, "DecisionCloseButton");
            return await dialog.ShowAsync();
        }
        finally { _dialogGate.Release(); }
    }
    private static Style CreateDialogButtonStyle(Style? baseStyle, string automationId)
    {
        var style = new Style(typeof(Button)) { BasedOn = baseStyle };
        style.Setters.Add(new Setter(Microsoft.UI.Xaml.Automation.AutomationProperties.AutomationIdProperty, automationId));
        return style;
    }
    private async Task<bool> ConfirmDraftAsync()
    {
        if (!_viewModel.HasUnsavedChanges) return true;
        var result = await DialogAsync("未保存の設定", "変更を保存しますか？", "保存", "破棄", "キャンセル");
        if (result == ContentDialogResult.Secondary) { _viewModel.CancelSettings(); return true; }
        return result == ContentDialogResult.Primary && await _viewModel.SaveSettingsAsync();
    }
    /// <summary>Confirms an explicit recovery retaining originals; no automatic deletion.</summary>
    public async Task<bool> ConfirmRecoveryAsync() => await DialogAsync("設定を復旧", "元のデータベースと付随ファイルを同じフォルダーに退避して、新しい設定を作成します。退避ファイルには暗号化されたキーが含まれます。続行しますか？", "復旧", "", "キャンセル") == ContentDialogResult.Primary;
    private async Task<CloseDecision> DecideCloseAsync()
    {
        var result = await DialogAsync("停止を完了できません", "停止が遅延または失敗しています。既存の停止処理が実行中なら完了を待ちます。強制終了では記録やリソース解放を保証できません。", "再試行 / 待機", "このまま終了", "終了をキャンセル");
        return result == ContentDialogResult.Primary ? CloseDecision.Retry : result == ContentDialogResult.Secondary ? CloseDecision.ExitAnyway : CloseDecision.Cancel;
    }
    private async void ClosingAsync(AppWindow sender, AppWindowClosingEventArgs e)
    {
        if (_canClose) return;
        e.Cancel = true;
        if (_isClosing || _isNavigating) return;
        _isClosing = true;
        try
        {
            if (await _close.TryCloseAsync()) { _canClose = true; Close(); }
        }
        finally { _isClosing = false; }
    }
}
