namespace SpeechTranslatorDesktop.Behaviors;

/// <summary>
/// ListBoxに新規アイテムが追加された時に自動的に最下部にスクロールするビヘイビア。
/// </summary>
public static class AutoScrollListBoxBehavior
{
    public static bool GetAutoScroll(ListBox listBox)
    {
        ArgumentNullException.ThrowIfNull(listBox);
        return (bool)listBox.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(ListBox listBox, bool value)
    {
        ArgumentNullException.ThrowIfNull(listBox);
        listBox.SetValue(AutoScrollProperty, value);
    }

    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
        "AutoScroll",
        typeof(bool),
        typeof(AutoScrollListBoxBehavior),
        new PropertyMetadata(false, OnAutoScrollPropertyChanged));

    private static void OnAutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            listBox.ItemContainerGenerator.ItemsChanged += (s, args) => ScrollToBottom(listBox);

            // ItemsSource が後から設定される場合に対応
            var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            descriptor?.AddValueChanged(listBox, (s, args) =>
            {
                if (listBox.ItemsSource is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += (sender, collectionArgs) => ScrollToBottom(listBox);
                }
            });
        }
    }

    private static void ScrollToBottom(ListBox listBox)
    {
        if (listBox.Items.Count == 0)
        {
            return;
        }

        listBox.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Render,
            () =>
            {
                var lastIndex = listBox.Items.Count - 1;
                listBox.ScrollIntoView(listBox.Items[lastIndex]);
            });
    }
}
