using System.Globalization;
using System.Windows;
using System.Windows.Media;
using F1Tracker.Data;
using F1Tracker.Helpers;

namespace F1Tracker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadDashboard();
    }

    private void LoadDashboard()
    {
        try
        {
            WelcomeText.Text = $"Добро пожаловать, {Session.Username}";
            RoleDescText.Text = Session.IsAdmin
                ? "Роль: администратор — полный доступ, управление пользователями и журналом."
                : "Роль: пользователь — просмотр и работа с данными.";

            TeamsCount.Text = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM teams;").ToString();
            DriversCount.Text = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM drivers;").ToString();
            GpCount.Text = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM grand_prix;").ToString();
            ResultsCount.Text = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM results;").ToString();

            LoadNextRace();

            LeadersList.ItemsSource = Database.ExecuteQuery(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY SUM(r.points_scored) DESC, d.driver_name) AS Pos,
                       d.driver_name AS Driver,
                       t.team_name   AS Team,
                       SUM(r.points_scored) AS Points
                FROM results r
                JOIN drivers d ON d.driver_id = r.driver_id
                JOIN teams   t ON t.team_id   = d.team_id
                WHERE r.session_type IN ('Race', 'Sprint')
                GROUP BY d.driver_id
                ORDER BY Pos
                LIMIT 5;
                """).DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось загрузить данные:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadNextRace()
    {
        var table = Database.ExecuteQuery(
            """
            SELECT gp_name, circuit, race_date, round_number, is_sprint
            FROM grand_prix
            WHERE race_date >= @today
            ORDER BY race_date
            LIMIT 1;
            """,
            Database.P("@today", DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

        if (table.Rows.Count == 0)
        {
            NextRaceName.Text = "Сезон завершён";
            NextRaceCircuit.Text = "Все Гран-при 2026 года уже прошли";
            NextRaceCountdown.Text = "—";
            NextRaceDate.Text = "";
            return;
        }

        var row = table.Rows[0];
        var date = DateTime.ParseExact((string)row["race_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        int days = (date - DateTime.Today).Days;

        NextRaceName.Text = (string)row["gp_name"];
        NextRaceCircuit.Text = $"Этап {row["round_number"]} · {row["circuit"]}" +
                               (Convert.ToInt32(row["is_sprint"]) == 1 ? " · СПРИНТ" : "");
        NextRaceDate.Text = date.ToString("dd.MM.yyyy");
        NextRaceCountdown.Text = days switch
        {
            0 => "СЕГОДНЯ",
            1 => "ЗАВТРА",
            _ => $"{days} дн."
        };
    }

    private void Calc_Click(object sender, RoutedEventArgs e)
    {
        // Валидация: целое число 1..22
        if (!int.TryParse(PositionBox.Text.Trim(), out int position) || position < 1 || position > 22)
        {
            CalcResult.FontSize = 14;
            CalcResult.Foreground = (Brush)FindResource("AccentBrush");
            CalcResult.Text = "Введите целое число от 1 до 22";
            return;
        }

        string session = SprintRadio.IsChecked == true ? "Sprint" : "Race";
        int points = Points.For(session, position);

        CalcResult.FontSize = 34;
        CalcResult.Foreground = (Brush)FindResource("TextBrush");
        CalcResult.Text = $"{points} очк.";
    }

    private void OpenData_Click(object sender, RoutedEventArgs e) => Navigator.GoTo(AppPage.Data, this);
    private void OpenStats_Click(object sender, RoutedEventArgs e) => Navigator.GoTo(AppPage.Statistics, this);
}