namespace F1Tracker.Helpers;

/// <summary>Сведения о проекте и авторе (единое место правки).</summary>
public static class AppInfo
{
    public const string AuthorName = "Фамилия Имя Отчество";   // ← ЗАМЕНИТЬ
    public const string Group = "Группа XX-00";           // ← ЗАМЕНИТЬ
    public const string Institution = "Название учебного заведения";   // ← ЗАМЕНИТЬ
    public const string Version = "1.0.0";
    public const string RepoUrl = "https://github.com/ВАШ_ЛОГИН/f1-tracker-2026";   // ← ЗАМЕНИТЬ

    public const string Description =
        "Десктопное приложение для учёта сезона Формулы-1 2026: справочники команд, пилотов " +
        "и Гран-при, ввод результатов сессий с автоматическим расчётом очков, турнирная таблица " +
        "пилотов и кубок конструкторов, отчёты в Excel и PDF, резервное копирование базы данных, " +
        "разграничение прав (admin / user) и журнал действий.";

    public static readonly string[] Stack =
    {
        "C# · .NET 8", "WPF · XAML", "SQLite", "Microsoft.Data.Sqlite",
        "BCrypt.Net-Next", "ClosedXML", "QuestPDF", "Git · GitHub"
    };
}