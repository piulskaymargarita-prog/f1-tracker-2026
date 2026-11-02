using System.Windows;
using F1Tracker.Data;
using F1Tracker.Views;

namespace F1Tracker.Helpers;

public enum AppPage { Main, Data, Statistics, Admin, About }

/// <summary>Переходы между окнами: открыть следующее на том же месте и закрыть текущее.</summary>
public static class Navigator
{
    public static void GoTo(AppPage page, Window current)
    {
        // Разграничение ролей: админский раздел — только для admin
        if (page == AppPage.Admin && !Session.IsAdmin)
        {
            ActionLogger.Log("ACCESS_DENIED", "Попытка открыть AdminWindow без прав");
            MessageBox.Show("Раздел доступен только администратору.", "Доступ запрещён",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Type target = page switch
        {
            AppPage.Main => typeof(MainWindow),
            AppPage.Data => typeof(DataWindow),
            AppPage.Statistics => typeof(StatisticsWindow),
            AppPage.Admin => typeof(AdminWindow),
            AppPage.About => typeof(AboutWindow),
            _ => typeof(MainWindow)
        };

        if (current.GetType() == target) return;   // уже здесь

        Switch(current, (Window)Activator.CreateInstance(target)!);
    }

    /// <summary>Переход в DataWindow с выбранной вкладкой и фильтром (из поиска).</summary>
    public static void GoToData(string tab, string filter, Window current) =>
        Switch(current, new DataWindow { InitialTab = tab, InitialFilter = filter });

    public static void Logout(Window current)
    {
        ActionLogger.Log("LOGOUT", "Выход из системы");
        Session.End();
        Switch(current, new LoginWindow());
    }

    private static void Switch(Window current, Window next)
    {
        // Окно входа маленькое, остальные — одного размера: границы копируем только между «большими»
        if (current is not LoginWindow && next is not LoginWindow)
        {
            if (current.WindowState == WindowState.Maximized)
            {
                next.WindowState = WindowState.Maximized;
            }
            else
            {
                next.WindowStartupLocation = WindowStartupLocation.Manual;
                next.Left = current.Left;
                next.Top = current.Top;
                next.Width = current.Width;
                next.Height = current.Height;
            }
        }

        Application.Current.MainWindow = next;
        next.Show();      // сначала показываем новое, потом закрываем старое — приложение не завершится
        current.Close();
    }
}