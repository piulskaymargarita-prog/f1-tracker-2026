using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using F1Tracker.Data;

namespace F1Tracker.Views.Controls;

public partial class AppFooter : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public AppFooter()
    {
        InitializeComponent();
        DbText.Text = $"БД: {System.IO.Path.GetFileName(Database.DbPath)}";

        _timer.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        Loaded += (_, _) => { ClockText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }
}