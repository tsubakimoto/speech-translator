using System.Windows;

namespace SpeechTranslatorDesktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public Ref<Languages> Speaker { get; } = new(Languages.English);

    public Ref<Languages> Translation { get; } = new(Languages.Japanese);

    public void Translate()
    {
        MessageBox.Show($"Translating from {Speaker.Value} to {Translation.Value}");
    }

    public void Stop()
    {
        MessageBox.Show("Stopping");
    }
}
