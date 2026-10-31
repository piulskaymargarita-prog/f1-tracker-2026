namespace F1Tracker.Data;

public static class AuthService
{
    /// <summary>Проверяет логин/пароль. При успехе заполняет Session и пишет в журнал.</summary>
    public static bool TryLogin(string username, string password)
    {
        var table = Database.ExecuteQuery(
            "SELECT user_id, username, password_hash, role FROM users WHERE username = @u;",
            Database.P("@u", username));

        if (table.Rows.Count == 0)
        {
            ActionLogger.LogAs(null, "LOGIN_FAILED", $"Неизвестный логин: {username}");
            return false;
        }

        var row = table.Rows[0];
        int userId = Convert.ToInt32(row["user_id"]);

        bool passwordOk;
        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(password, (string)row["password_hash"]);
        }
        catch (Exception)   // повреждённый хэш в БД
        {
            passwordOk = false;
        }

        if (!passwordOk)
        {
            ActionLogger.LogAs(userId, "LOGIN_FAILED", "Неверный пароль");
            return false;
        }

        Session.Start(userId, (string)row["username"], (string)row["role"]);
        ActionLogger.Log("LOGIN", $"Успешный вход, роль: {Session.Role}");
        return true;
    }
}