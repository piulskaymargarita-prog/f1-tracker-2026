namespace F1Tracker.Data;

/// <summary>Текущий авторизованный пользователь. Заполняется в LoginWindow (этап 5).</summary>
public static class Session
{
    public static int UserId { get; private set; }
    public static string Username { get; private set; } = string.Empty;
    public static string Role { get; private set; } = string.Empty;

    public static bool IsLoggedIn => UserId != 0;
    public static bool IsAdmin => Role == "admin";

    public static void Start(int userId, string username, string role)
    {
        UserId = userId;
        Username = username;
        Role = role;
    }

    public static void End()
    {
        UserId = 0;
        Username = string.Empty;
        Role = string.Empty;
    }
}
