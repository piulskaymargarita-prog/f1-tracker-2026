using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace F1Tracker.Helpers;

/// <summary>Общая настройка автогенерируемых колонок DataGrid и текстовый фильтр.</summary>
public static class GridHelper
{
    private static readonly Dictionary<string, string> Headers = new()
    {
        ["team_name"] = "КОМАНДА",
        ["principal"] = "РУКОВОДИТЕЛЬ",
        ["power_unit"] = "СИЛ. УСТАНОВКА",
        ["base_country"] = "СТРАНА БАЗЫ",
        ["number"] = "№",
        ["driver_name"] = "ПИЛОТ",
        ["country"] = "СТРАНА",
        ["team"] = "КОМАНДА",
        ["round_no"] = "ЭТАП",
        ["gp_name"] = "ГРАН-ПРИ",
        ["circuit"] = "ТРАССА",
        ["sprint"] = "СПРИНТ",
        ["race_date"] = "ДАТА",
        ["session"] = "СЕССИЯ",
        ["position"] = "ПОЗ.",
        ["points"] = "ОЧКИ",
        ["status"] = "СТАТУС",
        ["pos"] = "#",
        ["wins"] = "ПОБЕДЫ",
        ["podiums"] = "ПОДИУМЫ",
        ["login"] = "ЛОГИН",
        ["role"] = "РОЛЬ",
        ["created_at"] = "СОЗДАН",
        ["ts"] = "ВРЕМЯ",
        ["username"] = "ПОЛЬЗОВАТЕЛЬ",
        ["action"] = "ДЕЙСТВИЕ",
        ["details"] = "ДЕТАЛИ"
    };

    private static readonly HashSet<string> Narrow = new()
        { "number", "round_no", "position", "points", "pos", "wins", "podiums", "sprint" };

    private static readonly HashSet<string> Mono = new()
        { "number", "round_no", "race_date", "position", "points", "pos", "wins", "podiums", "ts", "created_at" };

    /// <summary>Вызывать из DataGrid.AutoGeneratingColumn.</summary>
    public static void Configure(DataGridAutoGeneratingColumnEventArgs e)
    {
        string col = e.PropertyName;

        // Служебные идентификаторы в таблице не показываем (читаем их из DataRowView)
        if (col == "id" || col.EndsWith("_id", StringComparison.Ordinal))
        {
            e.Cancel = true;
            return;
        }

        e.Column.Header = Headers.TryGetValue(col, out var header) ? header : col.ToUpperInvariant();
        e.Column.Width = Narrow.Contains(col)
            ? new DataGridLength(90)
            : new DataGridLength(1, DataGridLengthUnitType.Star);

        if (Mono.Contains(col) && e.Column is DataGridTextColumn textColumn)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty,
                                         Application.Current.FindResource("MonoFont")));
            textColumn.ElementStyle = style;
        }
    }

    /// <summary>Экранирует спецсимволы выражения DataView.RowFilter (LIKE).</summary>
    public static string EscapeLike(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\'': sb.Append("''"); break;
                case '[': case ']': case '%': case '*': sb.Append('[').Append(c).Append(']'); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Фильтр по подстроке в указанных колонках (или во всех, кроме id).</summary>
    public static void ApplyTextFilter(DataView view, string text, params string[] columns)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            view.RowFilter = "";
            return;
        }

        string esc = EscapeLike(text);
        string[] names = columns.Length > 0
            ? columns
            : view.Table!.Columns.Cast<DataColumn>()
                  .Select(c => c.ColumnName)
                  .Where(n => n != "id" && !n.EndsWith("_id", StringComparison.Ordinal))
                  .ToArray();

        view.RowFilter = string.Join(" OR ",
            names.Select(n => $"Convert([{n}], 'System.String') LIKE '%{esc}%'"));
    }
}