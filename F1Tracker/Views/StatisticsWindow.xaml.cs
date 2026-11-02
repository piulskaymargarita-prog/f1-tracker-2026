using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using F1Tracker.Data;
using F1Tracker.Helpers;
using F1Tracker.Reports;

namespace F1Tracker.Views;

public partial class StatisticsWindow : Window
{
    public StatisticsWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadAll();
    }

    private void LoadAll()
    {
        try
        {
            var drivers = ReportService.GetDriverStandings();
            var teams = ReportService.GetConstructorStandings();
            DriversGrid.ItemsSource = drivers.DefaultView;
            TeamsGrid.ItemsSource = teams.DefaultView;

            // Карточки лидеров
            var topDriver = drivers.Rows.Count > 0 ? drivers.Rows[0] : null;
            if (topDriver is not null && Convert.ToInt64(topDriver["points"]) > 0)
            {
                LeaderDriverText.Text = (string)topDriver["driver_name"];
                LeaderDriverSub.Text = $"{topDriver["points"]} очк. · {topDriver["team"]}";
            }
            else
            {
                LeaderDriverText.Text = "—";
                LeaderDriverSub.Text = "пока нет результатов";
            }

            var topTeam = teams.Rows.Count > 0 ? teams.Rows[0] : null;
            if (topTeam is not null && Convert.ToInt64(topTeam["points"]) > 0)
            {
                LeaderTeamText.Text = (string)topTeam["team"];
                LeaderTeamSub.Text = $"{topTeam["points"]} очк.";
            }
            else
            {
                LeaderTeamText.Text = "—";
                LeaderTeamSub.Text = "пока нет результатов";
            }

            long done = Database.ExecuteScalar<long>("SELECT COUNT(DISTINCT gp_id) FROM results;");
            long total = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM grand_prix;");
            GpDoneText.Text = $"{done} / {total}";

            // Для PDF предлагаем только Гран-при, у которых есть результаты
            GpCombo.ItemsSource = Database.ExecuteQuery(
                """
                SELECT gp_id, round_number || '. ' || gp_name AS title
                FROM grand_prix
                WHERE gp_id IN (SELECT gp_id FROM results)
                ORDER BY round_number;
                """).DefaultView;
            if (GpCombo.Items.Count > 0) GpCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось загрузить статистику:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Grid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) =>
        GridHelper.Configure(e);

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DriversGrid.ItemsSource is DataView view)
            GridHelper.ApplyTextFilter(view, FilterBox.Text, "driver_name", "team", "country");
    }

    // ===================== Excel =====================
    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить турнирную таблицу",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"F1_2026_standings_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            ReportService.ExportDriversExcel(dialog.FileName);
            ActionLogger.Log("EXPORT_EXCEL", dialog.FileName);
            AskOpen(dialog.FileName);
        }
        catch (IOException)
        {
            MessageBox.Show("Не удаётся записать файл. Возможно, он открыт в Excel: закройте его и повторите.",
                            "Экспорт в Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка экспорта:\n" + ex.Message, "Экспорт в Excel",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== PDF =====================
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (GpCombo.SelectedValue is not long gpId)
        {
            MessageBox.Show("Выберите Гран-при из списка. Если список пуст, сначала внесите результаты " +
                            "на странице «Данные → Результаты».", "Экспорт в PDF",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Сохранить результаты Гран-при",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"F1_2026_GP{gpId}_results_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            ReportService.ExportGpResultsPdf(gpId, dialog.FileName);
            ActionLogger.Log("EXPORT_PDF", $"gp {gpId}: {dialog.FileName}");
            AskOpen(dialog.FileName);
        }
        catch (IOException)
        {
            MessageBox.Show("Не удаётся записать файл. Возможно, он открыт в другой программе.",
                            "Экспорт в PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Экспорт в PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка экспорта:\n" + ex.Message, "Экспорт в PDF",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== Бэкап =====================
    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = ReportService.BackupDatabase();
            ActionLogger.Log("BACKUP_DB", path);

            var answer = MessageBox.Show($"Резервная копия создана:\n{path}\n\nПоказать файл в папке?",
                                         "Резервное копирование", MessageBoxButton.YesNo,
                                         MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
                Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось создать копию:\n" + ex.Message, "Резервное копирование",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void AskOpen(string path)
    {
        var answer = MessageBox.Show($"Файл сохранён:\n{path}\n\nОткрыть его?", "Готово",
                                     MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (answer == MessageBoxResult.Yes)
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}