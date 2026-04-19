namespace SpeechTranslatorDesktop.Behaviors;

/// <summary>
/// DataGridに新規アイテムが追加された時に自動的に最下部にスクロールするビヘイビア。
/// </summary>
public static class AutoScrollDataGridBehavior
{
    public static bool GetAutoScroll(DataGrid dataGrid)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);
        return (bool)dataGrid.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DataGrid dataGrid, bool value)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);
        dataGrid.SetValue(AutoScrollProperty, value);
    }

    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
        "AutoScroll",
        typeof(bool),
        typeof(AutoScrollDataGridBehavior),
        new PropertyMetadata(false, OnAutoScrollPropertyChanged));

    private static void OnAutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            dataGrid.ItemContainerGenerator.ItemsChanged += (s, args) => ScrollToBottom(dataGrid);

            // ItemsSource が後から設定される場合に対応
            var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid));
            descriptor?.AddValueChanged(dataGrid, (s, args) =>
            {
                if (dataGrid.ItemsSource is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += (sender, collectionArgs) => ScrollToBottom(dataGrid);
                }
            });
        }
    }

    private static void ScrollToBottom(DataGrid dataGrid)
    {
        if (dataGrid.Items.Count == 0)
        {
            return;
        }

        dataGrid.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Render,
            () =>
            {
                var lastIndex = dataGrid.Items.Count - 1;
                dataGrid.ScrollIntoView(dataGrid.Items[lastIndex]);
            });
    }
}
