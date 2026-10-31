using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace F1Tracker.Data;

/// <summary>Единая точка доступа к SQLite. Все запросы — только с параметрами (@name).</summary>
public static class Database
{
    public static string DbPath { get; } = Path.Combine(AppContext.BaseDirectory, "f1.db");

    private static string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DbPath,
        ForeignKeys = true
    }.ToString();

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Короткий конструктор параметра: Database.P("@id", 5). null → DBNull.</summary>
    public static SqliteParameter P(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    /// <summary>SELECT → DataTable (удобно для DataGrid.ItemsSource = table.DefaultView).</summary>
    public static DataTable ExecuteQuery(string sql, params SqliteParameter[] parameters)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);

        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    /// <summary>INSERT / UPDATE / DELETE → количество затронутых строк.</summary>
    public static int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>INSERT → id вставленной строки (last_insert_rowid на том же соединении).</summary>
    public static long ExecuteInsert(string sql, params SqliteParameter[] parameters)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        cmd.ExecuteNonQuery();

        using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        return (long)idCmd.ExecuteScalar()!;
    }

    /// <summary>SELECT одного значения (COUNT, SUM, ...). Если NULL — default(T).</summary>
    public static T? ExecuteScalar<T>(string sql, params SqliteParameter[] parameters)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);

        var result = cmd.ExecuteScalar();
        if (result is null || result is DBNull) return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }
}