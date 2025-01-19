using System.Windows;

using Microsoft.Extensions.Configuration;

namespace SpeechTranslatorDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }

        public App()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") ?? "Development";

            // https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/migration/?view=netdesktop-8.0
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
