using Microsoft.Data.Sqlite;

namespace F1Tracker.Data;

public static class Database
{
    public static string DbPath { get; } = Path.Combine(AppContext.BaseDirectory, "f1.db");

    private static string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DbPath,
        ForeignKeys = true          // включает PRAGMA foreign_keys = ON для каждого соединения
    }.ToString();

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}