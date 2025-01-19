using System.Windows;

using Microsoft.Extensions.Configuration;

using SpeechTranslatorShared;

namespace SpeechTranslatorDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Settings Settings { get; private set; }

        public App()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") ?? "Development";

            // https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/migration/?view=netdesktop-8.0
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>();
        }
    }
}
