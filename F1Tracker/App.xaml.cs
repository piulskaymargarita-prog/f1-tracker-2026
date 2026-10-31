using System.Windows;
using F1Tracker.Data;

namespace F1Tracker;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            DbInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось инициализировать базу данных:\n{ex.Message}",
                            "F1 Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }
}
