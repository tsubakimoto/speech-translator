using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using SpeechTranslatorDesktop.Behaviors;

namespace SpeechTranslator.Desktop.Tests;

public class PasswordBoxBindingBehaviorTests
{
    [Fact]
    public async Task BoundPassword_WhenInitialValueIsEmpty_UpdatesSourceAndRaisesPropertyChangedOnUserInput()
    {
        await RunOnStaThreadAsync(() =>
        {
            var source = new BindableSecret();
            var changedProperties = new List<string?>();
            source.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
            var passwordBox = CreateBoundPasswordBox(source);

            passwordBox.Password = "entered-key";

            source.Secret.Should().Be("entered-key");
            changedProperties.Should().Contain(nameof(BindableSecret.Secret));
        });
    }

    [Fact]
    public async Task BoundPassword_WhenSourceChanges_UpdatesPasswordBox()
    {
        await RunOnStaThreadAsync(() =>
        {
            var source = new BindableSecret();
            var passwordBox = CreateBoundPasswordBox(source);

            source.Secret = "loaded-key";

            passwordBox.Password.Should().Be("loaded-key");
        });
    }

    [Fact]
    public async Task BoundPassword_WhenPasswordIsCleared_UpdatesSourceToEmpty()
    {
        await RunOnStaThreadAsync(() =>
        {
            var source = new BindableSecret { Secret = "loaded-key" };
            var changedProperties = new List<string?>();
            source.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
            var passwordBox = CreateBoundPasswordBox(source);

            passwordBox.Password = string.Empty;

            source.Secret.Should().BeEmpty();
            changedProperties.Should().Contain(nameof(BindableSecret.Secret));
        });
    }

    private static PasswordBox CreateBoundPasswordBox(BindableSecret source)
    {
        var passwordBox = new PasswordBox();
        BindingOperations.SetBinding(
            passwordBox,
            PasswordBoxBindingBehavior.BoundPasswordProperty,
            new Binding(nameof(BindableSecret.Secret))
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

        return passwordBox;
    }

    private static Task RunOnStaThreadAsync(Action testAction)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                testAction();
                completionSource.SetResult();
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completionSource.Task;
    }

    private sealed class BindableSecret : INotifyPropertyChanged
    {
        private string _secret = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Secret
        {
            get => _secret;
            set
            {
                if (_secret == value)
                {
                    return;
                }

                _secret = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Secret)));
            }
        }
    }
}
