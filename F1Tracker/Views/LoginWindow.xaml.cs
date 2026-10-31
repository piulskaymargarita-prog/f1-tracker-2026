using System.Windows;
using F1Tracker.Data;

namespace F1Tracker.Views;

public partial class LoginWindow : Window
{
    private int _failedAttempts;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameBox.Text.Trim();
        string password = PasswordInput.Password;

        // Валидация: поля не пустые
        if (username.Length == 0 || password.Length == 0)
        {
            ErrorText.Text = "Введите логин и пароль.";
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Content = "ПРОВЕРКА…";
        ErrorText.Text = "";

        bool ok;
        try
        {
            // BCrypt.Verify медленный по замыслу — выносим из UI-потока
            ok = await Task.Run(() => AuthService.TryLogin(username, password));
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Ошибка базы данных: " + ex.Message;
            LoginButton.Content = "ВОЙТИ";
            LoginButton.IsEnabled = true;
            return;
        }

        if (ok)
        {
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
            return;
        }

        _failedAttempts++;
        PasswordInput.Clear();

        if (_failedAttempts >= 5)   // простая защита от перебора
        {
            ErrorText.Text = "Слишком много попыток. Подождите 10 секунд.";
            await Task.Delay(TimeSpan.FromSeconds(10));
            _failedAttempts = 0;
            ErrorText.Text = "";
        }
        else
        {
            ErrorText.Text = "Неверный логин или пароль.";
        }

        LoginButton.Content = "ВОЙТИ";
        LoginButton.IsEnabled = true;
        PasswordInput.Focus();
    }
}