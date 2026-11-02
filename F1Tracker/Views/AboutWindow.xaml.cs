using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using F1Tracker.Data;
using F1Tracker.Helpers;

namespace F1Tracker.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        VersionText.Text = $"Версия {AppInfo.Version} · учебный проект · сезон Формулы-1 2026";
        DescText.Text = AppInfo.Description;
        AuthorText.Text = AppInfo.AuthorName;
        GroupText.Text = AppInfo.Group;
        InstitutionText.Text = AppInfo.Institution;
        StackList.ItemsSource = AppInfo.Stack;

        Loaded += (_, _) => LoadStats();
    }

    private void LoadStats()
    {
        try
        {
            var table = Database.ExecuteQuery(
                """
                SELECT 'Пользователи' AS name, COUNT(*) AS cnt FROM users
                UNION ALL SELECT 'Команды', COUNT(*) FROM teams
                UNION ALL SELECT 'Пилоты', COUNT(*) FROM drivers
                UNION ALL SELECT 'Гран-при', COUNT(*) FROM grand_prix
                UNION ALL SELECT 'Результаты', COUNT(*) FROM results
                UNION ALL SELECT 'Записи журнала', COUNT(*) FROM action_log;
                """);

            StatsList.ItemsSource = table.DefaultView;

            long total = 0;
            foreach (System.Data.DataRow row in table.Rows)
                total += Convert.ToInt64(row["cnt"]);
            TotalText.Text = total.ToString();

            double kb = new FileInfo(Database.DbPath).Length / 1024.0;
            SizeText.Text = $"{kb:F1} КБ";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось загрузить статистику БД:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CommentBox_TextChanged(object sender, TextChangedEventArgs e) =>
        CounterText.Text = $"{CommentBox.Text.Length} / 300";

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        InfoText.Text = "";

        if (RatingCombo.SelectedValue is not string ratingText || !int.TryParse(ratingText, out int rating))
        {
            ErrorText.Text = "Выберите оценку от 1 до 5.";
            return;
        }

        string comment = CommentBox.Text.Trim();
        if (comment.Length < 10)
        {
            ErrorText.Text = "Комментарий слишком короткий: минимум 10 символов.";
            return;
        }

        ActionLogger.Log("FEEDBACK", $"{rating}/5: {comment}");

        CommentBox.Clear();
        RatingCombo.SelectedIndex = -1;

        long mine = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM action_log WHERE action = 'FEEDBACK' AND user_id = @u;",
            Database.P("@u", Session.UserId));
        InfoText.Text = $"✔ Спасибо! Отзыв сохранён (ваших отзывов: {mine}).";
        LoadStats();
    }

    private void OpenRepo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(AppInfo.RepoUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось открыть ссылку:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}