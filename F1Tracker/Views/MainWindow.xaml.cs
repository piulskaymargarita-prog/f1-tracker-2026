using System.Windows;
using F1Tracker.Data;

namespace F1Tracker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WhoText.Text = $"Вы вошли как: {Session.Username}";
        RoleText.Text = $"Роль: {Session.Role}";
        AdminOnlyText.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        ActionLogger.Log("LOGOUT", "Выход из системы");
        Session.End();

        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}