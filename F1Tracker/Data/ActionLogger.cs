namespace F1Tracker.Data;

/// <summary>Запись событий в action_log. Ошибка журналирования не должна ронять приложение.</summary>
public static class ActionLogger
{
    /// <summary>Лог от имени текущего пользователя из Session.</summary>
    public static void Log(string action, string? details = null) =>
        LogAs(Session.IsLoggedIn ? Session.UserId : null, action, details);

    /// <summary>Лог от имени конкретного пользователя (или null — аноним).</summary>
    public static void LogAs(int? userId, string action, string? details = null)
    {
        try
        {
            Database.ExecuteNonQuery(
                "INSERT INTO action_log (user_id, action, details) VALUES (@u, @a, @d);",
                Database.P("@u", userId),
                Database.P("@a", action),
                Database.P("@d", details));
        }
        catch
        {
            // журнал вторичен: молча пропускаем сбой
        }
    }
}