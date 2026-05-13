using System.Configuration;
using System.Data;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace BudgetBuddy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LiveCharts.Configure(config => config.AddSkiaSharp());
        }

        public App()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture =
                new System.Globalization.CultureInfo("en-US");
        }

    }


}