using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

using Tsubakimoto.SpeechTranslator.DesktopApp.Behaviors;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class PasswordBoxBindingBehaviorTests
{
    [Fact]
    public async Task BoundPassword_WhenInitialValueIsEmpty_UpdatesSourceAndRaisesPropertyChangedOnUserInputAsync()
    {
        await RunOnStaThreadAsync(() =>
        {
            // Arrange
            var source = new BindableSecret();
            var changedProperties = new List<string?>();
            source.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
            var passwordBox = CreateBoundPasswordBox(source);

            // Act
            passwordBox.Password = "entered-key";

            // Assert
            Assert.Equal("entered-key", source.Secret);
            Assert.Contains(nameof(BindableSecret.Secret), changedProperties);
        });
    }

    [Fact]
    public async Task BoundPassword_WhenSourceChanges_UpdatesPasswordBoxAsync()
    {
        await RunOnStaThreadAsync(() =>
        {
            // Arrange
            var source = new BindableSecret();
            var passwordBox = CreateBoundPasswordBox(source);

            // Act
            source.Secret = "loaded-key";

            // Assert
            Assert.Equal("loaded-key", passwordBox.Password);
        });
    }

    [Fact]
    public async Task BoundPassword_WhenPasswordIsCleared_UpdatesSourceToEmptyAsync()
    {
        await RunOnStaThreadAsync(() =>
        {
            // Arrange
            var source = new BindableSecret { Secret = "loaded-key" };
            var changedProperties = new List<string?>();
            source.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
            var passwordBox = CreateBoundPasswordBox(source);

            // Act
            passwordBox.Password = string.Empty;

            // Assert
            Assert.Empty(source.Secret);
            Assert.Contains(nameof(BindableSecret.Secret), changedProperties);
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