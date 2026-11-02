using ClosedXML.Excel;
using F1Tracker.Data;
using Microsoft.Data.Sqlite;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.IO;

namespace F1Tracker.Reports;

/// <summary>Агрегации очков, отчёты Excel/PDF и резервное копирование БД.</summary>
public static class ReportService
{
    static ReportService()
    {
        // Community-лицензия QuestPDF: бесплатно для учебных и небольших проектов
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ===================== Агрегации очков =====================
    public static DataTable GetDriverStandings() => Database.ExecuteQuery(
        """
        SELECT ROW_NUMBER() OVER (ORDER BY COALESCE(SUM(r.points_scored), 0) DESC, d.driver_name) AS pos,
               d.driver_name AS driver_name,
               d.driver_number AS number,
               t.team_name AS team,
               d.country AS country,
               COALESCE(SUM(r.points_scored), 0) AS points,
               COALESCE(SUM(CASE WHEN r.session_type = 'Race' AND r.position = 1 THEN 1 ELSE 0 END), 0) AS wins,
               COALESCE(SUM(CASE WHEN r.session_type = 'Race' AND r.position <= 3 THEN 1 ELSE 0 END), 0) AS podiums
        FROM drivers d
        JOIN teams t ON t.team_id = d.team_id
        LEFT JOIN results r ON r.driver_id = d.driver_id AND r.session_type IN ('Race', 'Sprint')
        GROUP BY d.driver_id
        ORDER BY pos;
        """);

    public static DataTable GetConstructorStandings() => Database.ExecuteQuery(
        """
        SELECT ROW_NUMBER() OVER (ORDER BY COALESCE(SUM(r.points_scored), 0) DESC, t.team_name) AS pos,
               t.team_name AS team,
               t.power_unit AS power_unit,
               COALESCE(SUM(r.points_scored), 0) AS points
        FROM teams t
        LEFT JOIN drivers d ON d.team_id = t.team_id
        LEFT JOIN results r ON r.driver_id = d.driver_id AND r.session_type IN ('Race', 'Sprint')
        GROUP BY t.team_id
        ORDER BY pos;
        """);

    // ===================== Excel (ClosedXML) =====================
    public static void ExportDriversExcel(string path)
    {
        using var wb = new XLWorkbook();

        FillSheet(wb.Worksheets.Add("Пилоты"),
                  "F1 2026 — Турнирная таблица пилотов",
                  new[] { "#", "Пилот", "№", "Команда", "Страна", "Очки", "Победы", "Подиумы" },
                  new[] { "pos", "driver_name", "number", "team", "country", "points", "wins", "podiums" },
                  GetDriverStandings());

        FillSheet(wb.Worksheets.Add("Кубок конструкторов"),
                  "F1 2026 — Кубок конструкторов",
                  new[] { "#", "Команда", "Силовая установка", "Очки" },
                  new[] { "pos", "team", "power_unit", "points" },
                  GetConstructorStandings());

        wb.SaveAs(path);
    }

    private static void FillSheet(IXLWorksheet ws, string title, string[] headers, string[] columns, DataTable data)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(2, 1).Value = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}";

        const int headerRow = 4;
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E10600");
        }

        for (int r = 0; r < data.Rows.Count; r++)
        {
            for (int c = 0; c < columns.Length; c++)
            {
                object value = data.Rows[r][columns[c]];
                var cell = ws.Cell(headerRow + 1 + r, c + 1);
                if (value is long number) cell.Value = Convert.ToInt32(number);
                else cell.Value = Convert.ToString(value) ?? "";
            }
        }

        var table = ws.Range(headerRow, 1, headerRow + Math.Max(data.Rows.Count, 1), headers.Length);
        table.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);
    }

    // ===================== PDF (QuestPDF) =====================
    public static void ExportGpResultsPdf(long gpId, string path)
    {
        var gp = Database.ExecuteQuery(
            "SELECT gp_name, circuit, country, round_number, race_date FROM grand_prix WHERE gp_id = @id;",
            Database.P("@id", gpId));
        if (gp.Rows.Count == 0)
            throw new InvalidOperationException("Гран-при не найден.");

        var results = Database.ExecuteQuery(
            """
            SELECT r.session_type, r.position, d.driver_number, d.driver_name, t.team_name,
                   r.points_scored, r.status
            FROM results r
            JOIN drivers d ON d.driver_id = r.driver_id
            JOIN teams t ON t.team_id = d.team_id
            WHERE r.gp_id = @id
            ORDER BY r.position;
            """,
            Database.P("@id", gpId));
        if (results.Rows.Count == 0)
            throw new InvalidOperationException("Для выбранного Гран-при пока нет результатов.");

        var g = gp.Rows[0];
        string title = (string)g["gp_name"];
        string subtitle = $"Этап {g["round_number"]} · {g["circuit"]} · {g["country"]} · {g["race_date"]}";

        var sessions = new (string Key, string Title)[]
        {
            ("Qualifying", "Квалификация"), ("Sprint", "Спринт"), ("Race", "Гонка")
        };
        var rows = results.Rows.Cast<DataRow>().ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"F1 2026 — {title}").FontSize(20).Bold().FontColor("#E10600");
                    col.Item().Text(subtitle).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E10600");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    foreach (var (key, sessionTitle) in sessions)
                    {
                        var part = rows.Where(r => (string)r["session_type"] == key).ToList();
                        if (part.Count == 0) continue;

                        col.Item().PaddingTop(14).Text(sessionTitle).FontSize(14).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(36);
                                c.ConstantColumn(30);
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.ConstantColumn(40);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                foreach (var head in new[] { "Поз.", "№", "Пилот", "Команда", "Очки", "Статус" })
                                    h.Cell().Element(HeaderCell).Text(head).FontColor(Colors.White).Bold();
                            });

                            foreach (var r in part)
                            {
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["position"]) ?? "");
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["driver_number"]) ?? "");
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["driver_name"]) ?? "");
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["team_name"]) ?? "");
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["points_scored"]) ?? "");
                                table.Cell().Element(BodyCell).Text(Convert.ToString(r["status"]) ?? "");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Сформировано {DateTime.Now:dd.MM.yyyy HH:mm} · стр. ").FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber();
                    t.Span(" из ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(path);
    }

    private static IContainer HeaderCell(IContainer c) => c.Background("#E10600").Padding(4);

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);

    // ===================== Резервная копия =====================
    /// <summary>Онлайн-бэкап SQLite в backups\backup_YYYYMMDD_HHMMSS.db. Возвращает путь к файлу.</summary>
    public static string BackupDatabase()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(dir);
        string target = Path.Combine(dir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        using var source = Database.CreateConnection();
        var cs = new SqliteConnectionStringBuilder { DataSource = target, Pooling = false }.ToString();
        using var destination = new SqliteConnection(cs);
        destination.Open();
        source.BackupDatabase(destination);

        return target;
    }
}