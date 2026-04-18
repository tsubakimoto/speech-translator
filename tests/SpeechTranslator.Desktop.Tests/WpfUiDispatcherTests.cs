using System.Windows.Threading;

namespace SpeechTranslator.Desktop.Tests;

public class WpfUiDispatcherTests
{
    [Fact]
    public void Constructor_ThrowsWhenDispatcherIsNull()
    {
        Action create = () => _ = new WpfUiDispatcher(null!);

        create.Should().Throw<ArgumentNullException>()
            .WithParameterName("dispatcher");
    }

    [Fact]
    public void Post_ExecutesActionOnDispatcher()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var sut = new WpfUiDispatcher(dispatcher);
        var executed = false;
        var frame = new DispatcherFrame();

        sut.Post(() =>
        {
            executed = true;
            frame.Continue = false;
        });

        Dispatcher.PushFrame(frame);

        executed.Should().BeTrue();
    }
}
