namespace SpeechTranslatorDesktop.Behaviors;

public static class PasswordBoxBindingBehavior
{
    private static readonly DependencyProperty IsSubscribedProperty = DependencyProperty.RegisterAttached(
        "IsSubscribed",
        typeof(bool),
        typeof(PasswordBoxBindingBehavior));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating",
        typeof(bool),
        typeof(PasswordBoxBindingBehavior));

    public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
        "Attach",
        typeof(bool),
        typeof(PasswordBoxBindingBehavior),
        new PropertyMetadata(false, OnAttachChanged));

    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword",
        typeof(string),
        typeof(PasswordBoxBindingBehavior),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    public static bool GetAttach(DependencyObject dependencyObject)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        return (bool)dependencyObject.GetValue(AttachProperty);
    }

    public static void SetAttach(DependencyObject dependencyObject, bool value)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        dependencyObject.SetValue(AttachProperty, value);
    }

    public static string GetBoundPassword(DependencyObject dependencyObject)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        return dependencyObject.GetValue(BoundPasswordProperty) as string ?? string.Empty;
    }

    public static void SetBoundPassword(DependencyObject dependencyObject, string value)
    {
        ArgumentNullException.ThrowIfNull(dependencyObject);
        dependencyObject.SetValue(BoundPasswordProperty, value);
    }

    private static void OnAttachChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        if (e.NewValue is true)
        {
            EnsureSubscribed(passwordBox);
            UpdatePasswordBox(passwordBox, GetBoundPassword(passwordBox));
            return;
        }

        EnsureUnsubscribed(passwordBox);
    }

    private static void OnBoundPasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        if (!(bool)passwordBox.GetValue(IsUpdatingProperty))
        {
            UpdatePasswordBox(passwordBox, e.NewValue as string ?? string.Empty);
        }

        EnsureSubscribed(passwordBox);
    }

    private static void EnsureSubscribed(PasswordBox passwordBox)
    {
        if ((bool)passwordBox.GetValue(IsSubscribedProperty))
        {
            return;
        }

        passwordBox.PasswordChanged += HandlePasswordChanged;
        passwordBox.SetValue(IsSubscribedProperty, true);
    }

    private static void EnsureUnsubscribed(PasswordBox passwordBox)
    {
        if (!(bool)passwordBox.GetValue(IsSubscribedProperty))
        {
            return;
        }

        passwordBox.PasswordChanged -= HandlePasswordChanged;
        passwordBox.SetValue(IsSubscribedProperty, false);
    }

    private static void UpdatePasswordBox(PasswordBox passwordBox, string password)
    {
        if (passwordBox.Password == password)
        {
            return;
        }

        passwordBox.Password = password;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        passwordBox.SetValue(IsUpdatingProperty, false);
    }
}
