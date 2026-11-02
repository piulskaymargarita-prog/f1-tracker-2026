using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using F1Tracker.Data;
using F1Tracker.Helpers;

namespace F1Tracker.Views;

public partial class DataWindow : Window
{
    /// <summary>Заполняются из сквозного поиска (Navigator.GoToData).</summary>
    public string InitialTab { get; set; } = "teams";
    public string InitialFilter { get; set; } = "";

    private static readonly Dictionary<string, string> TabTitles = new()
    {
        ["teams"] = "Команды",
        ["drivers"] = "Пилоты",
        ["grand_prix"] = "Гран-при",
        ["results"] = "Результаты"
    };

    private string _tab = "teams";
    private long? _editId;      // null → режим добавления
    private bool _ready;

    /// <summary>Справочники правит admin; результаты вводят и admin, и user.</summary>
    private bool CanEdit => _tab == "results" || Session.IsAdmin;

    public DataWindow()
    {
        InitializeComponent();

        ResPositionBox.TextChanged += (_, _) => UpdatePoints();
        ResSessionCombo.SelectionChanged += (_, _) => UpdatePoints();

        Loaded += (_, _) =>
        {
            SwitchTab(TabTitles.ContainsKey(InitialTab) ? InitialTab : "teams");
            _ready = true;
            FilterBox.Text = InitialFilter;
            ApplyFilter();
        };
    }

    // ===================== Вкладки и загрузка =====================
    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: string tab } && tab != _tab)
            SwitchTab(tab);
    }

    private void SwitchTab(string tab)
    {
        _tab = tab;

        foreach (var b in TabsPanel.Children.OfType<Button>())
            b.Tag = (string)b.CommandParameter == tab ? "active" : null;

        TeamForm.Visibility = tab == "teams" ? Visibility.Visible : Visibility.Collapsed;
        DriverForm.Visibility = tab == "drivers" ? Visibility.Visible : Visibility.Collapsed;
        GpForm.Visibility = tab == "grand_prix" ? Visibility.Visible : Visibility.Collapsed;
        ResultForm.Visibility = tab == "results" ? Visibility.Visible : Visibility.Collapsed;

        Crumbs.Trail = $"Главная/Данные/{TabTitles[tab]}";
        FormBody.IsEnabled = CanEdit;
        ReadOnlyNote.Visibility = CanEdit ? Visibility.Collapsed : Visibility.Visible;

        FilterBox.Clear();
        LoadTab();
        ClearForm();
    }

    private void LoadTab()
    {
        try
        {
            LoadLookups();

            string sql = _tab switch
            {
                "teams" => """
                    SELECT team_id AS id, team_name, team_principal AS principal, power_unit, base_country
                    FROM teams
                    ORDER BY team_id;
                    """,
                "drivers" => """
                    SELECT d.driver_id AS id, d.driver_number AS number, d.driver_name, d.country,
                           t.team_name AS team, d.team_id
                    FROM drivers d
                    JOIN teams t ON t.team_id = d.team_id
                    ORDER BY d.driver_number;
                    """,
                "grand_prix" => """
                    SELECT gp_id AS id, round_number AS round_no, gp_name, circuit, country,
                           CASE WHEN is_sprint = 1 THEN 'Да' ELSE 'Нет' END AS sprint,
                           race_date
                    FROM grand_prix
                    ORDER BY round_number;
                    """,
                _ => """
                    SELECT r.result_id AS id, g.round_number AS round_no, g.gp_name,
                           r.session_type AS session, r.position, d.driver_name,
                           t.team_name AS team, r.points_scored AS points, r.status,
                           r.gp_id, r.driver_id
                    FROM results r
                    JOIN grand_prix g ON g.gp_id = r.gp_id
                    JOIN drivers d ON d.driver_id = r.driver_id
                    JOIN teams t ON t.team_id = d.team_id
                    ORDER BY g.round_number, r.session_type, r.position;
                    """
            };

            RecordsGrid.ItemsSource = Database.ExecuteQuery(sql).DefaultView;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось загрузить данные:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Списки для выпадающих полей форм.</summary>
    private void LoadLookups()
    {
        DriverTeamCombo.ItemsSource = Database.ExecuteQuery(
            "SELECT team_id, team_name FROM teams ORDER BY team_name;").DefaultView;
        ResGpCombo.ItemsSource = Database.ExecuteQuery(
            "SELECT gp_id, round_number || '. ' || gp_name AS title FROM grand_prix ORDER BY round_number;").DefaultView;
        ResDriverCombo.ItemsSource = Database.ExecuteQuery(
            "SELECT driver_id, driver_name FROM drivers ORDER BY driver_name;").DefaultView;
    }

    // ===================== Фильтр и колонки =====================
    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready) ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (RecordsGrid.ItemsSource is not DataView view) return;
        GridHelper.ApplyTextFilter(view, FilterBox.Text);
        CountText.Text = $"ЗАПИСЕЙ: {view.Count}";
    }

    private void RecordsGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) =>
        GridHelper.Configure(e);

    // ===================== Форма =====================
    private void RecordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is DataRowView row) FillForm(row);
    }

    private void FillForm(DataRowView r)
    {
        _editId = Convert.ToInt64(r["id"]);
        ErrorText.Text = "";
        InfoText.Text = "";
        FormTitle.Text = $"РЕДАКТИРОВАНИЕ · ID {_editId}";

        switch (_tab)
        {
            case "teams":
                TeamNameBox.Text = (string)r["team_name"];
                PrincipalBox.Text = (string)r["principal"];
                PowerUnitBox.Text = (string)r["power_unit"];
                BaseCountryBox.Text = (string)r["base_country"];
                break;

            case "drivers":
                DriverNameBox.Text = (string)r["driver_name"];
                DriverNumberBox.Text = Convert.ToString(r["number"]);
                DriverCountryBox.Text = (string)r["country"];
                DriverTeamCombo.SelectedValue = Convert.ToInt64(r["team_id"]);
                break;

            case "grand_prix":
                GpNameBox.Text = (string)r["gp_name"];
                CircuitBox.Text = (string)r["circuit"];
                GpCountryBox.Text = (string)r["country"];
                RoundBox.Text = Convert.ToString(r["round_no"]);
                RaceDateBox.Text = (string)r["race_date"];
                SprintCheck.IsChecked = (string)r["sprint"] == "Да";
                break;

            default:
                ResGpCombo.SelectedValue = Convert.ToInt64(r["gp_id"]);
                ResSessionCombo.SelectedValue = (string)r["session"];
                ResDriverCombo.SelectedValue = Convert.ToInt64(r["driver_id"]);
                ResPositionBox.Text = Convert.ToString(r["position"]);
                ResStatusBox.Text = (string)r["status"];
                break;
        }
    }

    private void ClearForm()
    {
        _editId = null;
        ErrorText.Text = "";
        InfoText.Text = "";
        FormTitle.Text = "НОВАЯ ЗАПИСЬ";
        RecordsGrid.SelectedItem = null;

        TeamNameBox.Clear(); PrincipalBox.Clear(); PowerUnitBox.Clear(); BaseCountryBox.Clear();
        DriverNameBox.Clear(); DriverNumberBox.Clear(); DriverCountryBox.Clear();
        DriverTeamCombo.SelectedIndex = -1;
        GpNameBox.Clear(); CircuitBox.Clear(); GpCountryBox.Clear(); RoundBox.Clear(); RaceDateBox.Clear();
        SprintCheck.IsChecked = false;
        ResGpCombo.SelectedIndex = -1;
        ResSessionCombo.SelectedIndex = 0;
        ResDriverCombo.SelectedIndex = -1;
        ResPositionBox.Clear();
        ResStatusBox.Text = "Finished";
        ResPointsText.Text = "—";
    }

    private void UpdatePoints()
    {
        if (!int.TryParse(ResPositionBox.Text.Trim(), out int pos) ||
            ResSessionCombo.SelectedValue is not string session)
        {
            ResPointsText.Text = "—";
            return;
        }
        ResPointsText.Text = $"{Points.For(session, pos)} очк.";
    }

    private void New_Click(object sender, RoutedEventArgs e) => ClearForm();

    private bool Fail(string message)
    {
        ErrorText.Text = message;
        InfoText.Text = "";
        return false;
    }

    // ===================== Сохранение =====================
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        InfoText.Text = "";

        if (!CanEdit)
        {
            Fail("Недостаточно прав для изменения этих данных.");
            return;
        }

        try
        {
            bool ok = _tab switch
            {
                "teams" => SaveTeam(),
                "drivers" => SaveDriver(),
                "grand_prix" => SaveGp(),
                _ => SaveResult()
            };
            if (!ok) return;

            LoadTab();
            ClearForm();
            InfoText.Text = "✔ Запись сохранена";
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Fail("Такая запись уже существует или нарушена связь между таблицами.");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }

    private bool SaveTeam()
    {
        string name = TeamNameBox.Text.Trim(), principal = PrincipalBox.Text.Trim(),
               unit = PowerUnitBox.Text.Trim(), country = BaseCountryBox.Text.Trim();

        if (name.Length == 0 || principal.Length == 0 || unit.Length == 0 || country.Length == 0)
            return Fail("Заполните все поля команды.");

        long dup = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM teams WHERE LOWER(team_name) = LOWER(@n) AND team_id <> @id;",
            Database.P("@n", name), Database.P("@id", _editId ?? 0));
        if (dup > 0) return Fail("Команда с таким названием уже есть.");

        if (_editId is null)
        {
            long id = Database.ExecuteInsert(
                "INSERT INTO teams (team_name, team_principal, power_unit, base_country) VALUES (@n, @p, @u, @c);",
                Database.P("@n", name), Database.P("@p", principal), Database.P("@u", unit), Database.P("@c", country));
            ActionLogger.Log("CREATE_TEAM", $"{name} (id {id})");
        }
        else
        {
            Database.ExecuteNonQuery(
                "UPDATE teams SET team_name = @n, team_principal = @p, power_unit = @u, base_country = @c WHERE team_id = @id;",
                Database.P("@n", name), Database.P("@p", principal), Database.P("@u", unit),
                Database.P("@c", country), Database.P("@id", _editId));
            ActionLogger.Log("UPDATE_TEAM", $"{name} (id {_editId})");
        }
        return true;
    }

    private bool SaveDriver()
    {
        string name = DriverNameBox.Text.Trim(), country = DriverCountryBox.Text.Trim();

        if (name.Length == 0 || country.Length == 0)
            return Fail("Укажите имя и страну пилота.");
        if (!int.TryParse(DriverNumberBox.Text.Trim(), out int number) || number < 1 || number > 99)
            return Fail("Номер пилота — целое число от 1 до 99.");
        if (DriverTeamCombo.SelectedValue is not long teamId)
            return Fail("Выберите команду.");

        long dup = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM drivers WHERE driver_number = @n AND driver_id <> @id;",
            Database.P("@n", number), Database.P("@id", _editId ?? 0));
        if (dup > 0) return Fail($"Номер {number} уже занят другим пилотом.");

        if (_editId is null)
        {
            long id = Database.ExecuteInsert(
                "INSERT INTO drivers (driver_name, driver_number, country, team_id) VALUES (@n, @num, @c, @t);",
                Database.P("@n", name), Database.P("@num", number), Database.P("@c", country), Database.P("@t", teamId));
            ActionLogger.Log("CREATE_DRIVER", $"{name} #{number} (id {id})");
        }
        else
        {
            Database.ExecuteNonQuery(
                "UPDATE drivers SET driver_name = @n, driver_number = @num, country = @c, team_id = @t WHERE driver_id = @id;",
                Database.P("@n", name), Database.P("@num", number), Database.P("@c", country),
                Database.P("@t", teamId), Database.P("@id", _editId));
            ActionLogger.Log("UPDATE_DRIVER", $"{name} #{number} (id {_editId})");
        }
        return true;
    }

    private bool SaveGp()
    {
        string name = GpNameBox.Text.Trim(), circuit = CircuitBox.Text.Trim(), country = GpCountryBox.Text.Trim();

        if (name.Length == 0 || circuit.Length == 0 || country.Length == 0)
            return Fail("Заполните название, трассу и страну.");
        if (!int.TryParse(RoundBox.Text.Trim(), out int round) || round < 1 || round > 30)
            return Fail("Номер этапа — целое число от 1 до 30.");
        if (!DateTime.TryParseExact(RaceDateBox.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out DateTime date))
            return Fail("Дата — в формате ГГГГ-ММ-ДД, например 2026-03-08.");
        if (date.Year != 2026)
            return Fail("Сезон 2026: дата должна быть в 2026 году.");

        long dup = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM grand_prix WHERE round_number = @r AND gp_id <> @id;",
            Database.P("@r", round), Database.P("@id", _editId ?? 0));
        if (dup > 0) return Fail($"Этап №{round} уже существует.");

        string dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        int sprint = SprintCheck.IsChecked == true ? 1 : 0;

        if (_editId is null)
        {
            long id = Database.ExecuteInsert(
                "INSERT INTO grand_prix (gp_name, circuit, country, round_number, is_sprint, race_date) " +
                "VALUES (@n, @c, @co, @r, @s, @d);",
                Database.P("@n", name), Database.P("@c", circuit), Database.P("@co", country),
                Database.P("@r", round), Database.P("@s", sprint), Database.P("@d", dateText));
            ActionLogger.Log("CREATE_GP", $"{name}, этап {round} (id {id})");
        }
        else
        {
            Database.ExecuteNonQuery(
                "UPDATE grand_prix SET gp_name = @n, circuit = @c, country = @co, round_number = @r, " +
                "is_sprint = @s, race_date = @d WHERE gp_id = @id;",
                Database.P("@n", name), Database.P("@c", circuit), Database.P("@co", country),
                Database.P("@r", round), Database.P("@s", sprint), Database.P("@d", dateText),
                Database.P("@id", _editId));
            ActionLogger.Log("UPDATE_GP", $"{name}, этап {round} (id {_editId})");
        }
        return true;
    }

    private bool SaveResult()
    {
        if (ResGpCombo.SelectedValue is not long gpId) return Fail("Выберите Гран-при.");
        if (ResSessionCombo.SelectedValue is not string session) return Fail("Выберите сессию.");
        if (ResDriverCombo.SelectedValue is not long driverId) return Fail("Выберите пилота.");
        if (!int.TryParse(ResPositionBox.Text.Trim(), out int pos) || pos < 1 || pos > 22)
            return Fail("Позиция — целое число от 1 до 22.");

        string status = ResStatusBox.Text.Trim();
        if (status.Length == 0) return Fail("Укажите статус (например, Finished).");

        if (session == "Sprint" &&
            Database.ExecuteScalar<long>("SELECT is_sprint FROM grand_prix WHERE gp_id = @g;",
                                         Database.P("@g", gpId)) == 0)
            return Fail("У выбранного Гран-при нет спринта.");

        long taken = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM results WHERE gp_id = @g AND session_type = @s AND position = @p AND result_id <> @id;",
            Database.P("@g", gpId), Database.P("@s", session), Database.P("@p", pos), Database.P("@id", _editId ?? 0));
        if (taken > 0) return Fail($"Позиция {pos} в этой сессии уже занята.");

        int points = Points.For(session, pos);

        if (_editId is null)
        {
            long id = Database.ExecuteInsert(
                "INSERT INTO results (gp_id, driver_id, session_type, position, points_scored, status) " +
                "VALUES (@g, @d, @s, @p, @pts, @st);",
                Database.P("@g", gpId), Database.P("@d", driverId), Database.P("@s", session),
                Database.P("@p", pos), Database.P("@pts", points), Database.P("@st", status));
            ActionLogger.Log("CREATE_RESULT", $"gp {gpId}, {session}, pos {pos}, pts {points} (id {id})");
        }
        else
        {
            Database.ExecuteNonQuery(
                "UPDATE results SET gp_id = @g, driver_id = @d, session_type = @s, position = @p, " +
                "points_scored = @pts, status = @st WHERE result_id = @id;",
                Database.P("@g", gpId), Database.P("@d", driverId), Database.P("@s", session),
                Database.P("@p", pos), Database.P("@pts", points), Database.P("@st", status),
                Database.P("@id", _editId));
            ActionLogger.Log("UPDATE_RESULT", $"gp {gpId}, {session}, pos {pos}, pts {points} (id {_editId})");
        }
        return true;
    }

    // ===================== Удаление с подтверждением =====================
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        InfoText.Text = "";

        if (!CanEdit) { Fail("Недостаточно прав для удаления."); return; }
        if (_editId is null || RecordsGrid.SelectedItem is not DataRowView row)
        {
            Fail("Сначала выберите запись в таблице.");
            return;
        }

        string sql, what;
        string? countSql = null;

        switch (_tab)
        {
            case "teams":
                sql = "DELETE FROM teams WHERE team_id = @id;";
                what = $"команду «{row["team_name"]}»";
                break;
            case "drivers":
                sql = "DELETE FROM drivers WHERE driver_id = @id;";
                what = $"пилота «{row["driver_name"]}»";
                countSql = "SELECT COUNT(*) FROM results WHERE driver_id = @id;";
                break;
            case "grand_prix":
                sql = "DELETE FROM grand_prix WHERE gp_id = @id;";
                what = $"Гран-при «{row["gp_name"]}»";
                countSql = "SELECT COUNT(*) FROM results WHERE gp_id = @id;";
                break;
            default:
                sql = "DELETE FROM results WHERE result_id = @id;";
                what = $"результат: {row["driver_name"]}, {row["session"]}, позиция {row["position"]}";
                break;
        }

        string warning = "";
        if (countSql is not null)
        {
            long n = Database.ExecuteScalar<long>(countSql, Database.P("@id", _editId));
            if (n > 0) warning = $"\n\nВместе с ним будут удалены результаты: {n}.";
        }

        var answer = MessageBox.Show($"Удалить {what}?{warning}", "Подтверждение удаления",
                                     MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            Database.ExecuteNonQuery(sql, Database.P("@id", _editId));
            ActionLogger.Log("DELETE_" + _tab.ToUpperInvariant(), $"{what} (id {_editId})");
            LoadTab();
            ClearForm();
            InfoText.Text = "✔ Запись удалена";
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Fail("Нельзя удалить: на запись ссылаются другие данные (например, у команды есть пилоты).");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }
}