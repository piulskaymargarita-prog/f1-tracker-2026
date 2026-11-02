using System.Data;
using F1Tracker.Models;

namespace F1Tracker.Data;

public static class SearchService
{
    // Один параметр @q, спецсимволы LIKE экранируются, SQL без конкатенации данных
    private const string Sql = """
        SELECT 'teams' AS tab, team_name AS title, team_principal || ' · ' || power_unit AS subtitle
        FROM teams
        WHERE team_name LIKE @q ESCAPE '\' OR team_principal LIKE @q ESCAPE '\' OR power_unit LIKE @q ESCAPE '\'
        UNION ALL
        SELECT 'drivers', driver_name, '#' || driver_number || ' · ' || country
        FROM drivers
        WHERE driver_name LIKE @q ESCAPE '\' OR country LIKE @q ESCAPE '\' OR CAST(driver_number AS TEXT) LIKE @q ESCAPE '\'
        UNION ALL
        SELECT 'grand_prix', gp_name, circuit || ' · ' || race_date
        FROM grand_prix
        WHERE gp_name LIKE @q ESCAPE '\' OR circuit LIKE @q ESCAPE '\' OR country LIKE @q ESCAPE '\'
        LIMIT 15;
        """;

    public static List<SearchHit> Search(string query)
    {
        var q = query.Trim();
        if (q.Length < 2) return new List<SearchHit>();

        string pattern = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var table = Database.ExecuteQuery(Sql, Database.P("@q", pattern));

        var list = new List<SearchHit>();
        foreach (DataRow r in table.Rows)
        {
            string tab = (string)r["tab"];
            string kind = tab switch { "teams" => "Команда", "drivers" => "Пилот", _ => "Гран-при" };
            list.Add(new SearchHit(tab, kind, (string)r["title"], (string)r["subtitle"]));
        }
        return list;
    }
}