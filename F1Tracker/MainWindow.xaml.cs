using System.Windows;
using F1Tracker.Data;

namespace F1Tracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var teams   = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM teams;");
        var drivers = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM drivers;");
        var gps     = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM grand_prix;");

        InfoText.Text = $"Команд: {teams} | Пилотов: {drivers} | Гран-при: {gps} | " +
                        $"Сессия: {(Session.IsLoggedIn ? Session.Username : "не авторизован")}";

        // Параметризованный запрос: пилоты команд с team_id >= @min
        DriversGrid.ItemsSource = Database.ExecuteQuery(
            """
            SELECT d.driver_number AS Number, d.driver_name AS Driver,
                   d.country AS Country, t.team_name AS Team
            FROM drivers d
            JOIN teams t ON t.team_id = d.team_id
            WHERE d.team_id >= @min
            ORDER BY t.team_id, d.driver_number;
            """,
            Database.P("@min", 1)).DefaultView;
    }
}