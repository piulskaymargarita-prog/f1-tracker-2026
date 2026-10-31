using Microsoft.Data.Sqlite;

namespace F1Tracker.Data;

public static class DbInitializer
{
    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS users (
            user_id       INTEGER PRIMARY KEY AUTOINCREMENT,
            username      TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            role          TEXT NOT NULL CHECK (role IN ('admin','user')),
            created_at    TEXT NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS teams (
            team_id        INTEGER PRIMARY KEY AUTOINCREMENT,
            team_name      TEXT NOT NULL,
            team_principal TEXT NOT NULL,
            power_unit     TEXT NOT NULL,
            base_country   TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS drivers (
            driver_id     INTEGER PRIMARY KEY AUTOINCREMENT,
            driver_name   TEXT NOT NULL,
            driver_number INTEGER NOT NULL UNIQUE CHECK (driver_number BETWEEN 1 AND 99),
            country       TEXT NOT NULL,
            team_id       INTEGER NOT NULL,
            FOREIGN KEY (team_id) REFERENCES teams(team_id) ON DELETE RESTRICT
        );

        CREATE TABLE IF NOT EXISTS grand_prix (
            gp_id        INTEGER PRIMARY KEY AUTOINCREMENT,
            gp_name      TEXT NOT NULL,
            circuit      TEXT NOT NULL,
            country      TEXT NOT NULL,
            round_number INTEGER NOT NULL UNIQUE CHECK (round_number BETWEEN 1 AND 30),
            is_sprint    INTEGER NOT NULL DEFAULT 0 CHECK (is_sprint IN (0,1)),
            race_date    TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS results (
            result_id     INTEGER PRIMARY KEY AUTOINCREMENT,
            gp_id         INTEGER NOT NULL,
            driver_id     INTEGER NOT NULL,
            session_type  TEXT NOT NULL CHECK (session_type IN ('Race','Sprint','Qualifying')),
            position      INTEGER NOT NULL CHECK (position BETWEEN 1 AND 22),
            points_scored INTEGER NOT NULL DEFAULT 0 CHECK (points_scored >= 0),
            status        TEXT NOT NULL DEFAULT 'Finished',
            UNIQUE (gp_id, driver_id, session_type),
            FOREIGN KEY (gp_id)     REFERENCES grand_prix(gp_id) ON DELETE CASCADE,
            FOREIGN KEY (driver_id) REFERENCES drivers(driver_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS action_log (
            log_id    INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id   INTEGER,
            action    TEXT NOT NULL,
            details   TEXT,
            timestamp TEXT NOT NULL DEFAULT (datetime('now','localtime')),
            FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL
        );

        CREATE INDEX IF NOT EXISTS idx_results_gp     ON results(gp_id);
        CREATE INDEX IF NOT EXISTS idx_results_driver ON results(driver_id);
        CREATE INDEX IF NOT EXISTS idx_drivers_team   ON drivers(team_id);
        """;

    /// <summary>Создаёт файл БД и таблицы; если команд ещё нет — заполняет тестовыми данными.</summary>
    public static void Initialize()
    {
        using var conn = Database.CreateConnection();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = SchemaSql;
            cmd.ExecuteNonQuery();
        }

        long teamCount;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM teams;";
            teamCount = (long)cmd.ExecuteScalar()!;
        }

        if (teamCount == 0)
            Seed(conn);
    }

    private static void Exec(SqliteConnection conn, SqliteTransaction tx, string sql,
                             params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void Seed(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();

        // ---------- users (пароли только как BCrypt-хэш) ----------
        const string userSql = "INSERT INTO users (username, password_hash, role) VALUES (@u, @h, @r);";
        Exec(conn, tx, userSql, ("@u", "admin"), ("@h", BCrypt.Net.BCrypt.HashPassword("Admin123!")), ("@r", "admin"));
        Exec(conn, tx, userSql, ("@u", "user"),  ("@h", BCrypt.Net.BCrypt.HashPassword("User123!")),  ("@r", "user"));

        // ---------- teams: team_id = 1..11 по порядку ----------
        var teams = new (string Name, string Principal, string PowerUnit, string Country)[]
        {
            ("Mercedes",        "Toto Wolff",        "Mercedes",      "United Kingdom"), // 1
            ("Ferrari",         "Frédéric Vasseur",  "Ferrari",       "Italy"),          // 2
            ("McLaren",         "Andrea Stella",     "Mercedes",      "United Kingdom"), // 3
            ("Red Bull Racing", "Laurent Mekies",    "Red Bull Ford", "United Kingdom"), // 4
            ("Racing Bulls",    "Alan Permane",      "Red Bull Ford", "Italy"),          // 5
            ("Aston Martin",    "Mike Krack",        "Honda",         "United Kingdom"), // 6
            ("Alpine",          "Steve Nielsen",     "Mercedes",      "France"),         // 7
            ("Williams",        "James Vowles",      "Mercedes",      "United Kingdom"), // 8
            ("Haas",            "Ayao Komatsu",      "Ferrari",       "United States"),  // 9
            ("Audi",            "Jonathan Wheatley", "Audi",          "Switzerland"),    // 10
            ("Cadillac",        "Graeme Lowdon",     "Ferrari",       "United States"),  // 11
        };
        foreach (var t in teams)
            Exec(conn, tx,
                "INSERT INTO teams (team_name, team_principal, power_unit, base_country) VALUES (@n, @p, @pu, @c);",
                ("@n", t.Name), ("@p", t.Principal), ("@pu", t.PowerUnit), ("@c", t.Country));

        // ---------- drivers: driver_id = 1..22 по порядку ----------
        var drivers = new (string Name, int Number, string Country, int TeamId)[]
        {
            ("George Russell",    63, "United Kingdom", 1), ("Kimi Antonelli",   12, "Italy",          1),
            ("Charles Leclerc",   16, "Monaco",         2), ("Lewis Hamilton",   44, "United Kingdom", 2),
            ("Lando Norris",       1, "United Kingdom", 3), ("Oscar Piastri",    81, "Australia",      3),
            ("Max Verstappen",     3, "Netherlands",    4), ("Isack Hadjar",      6, "France",         4),
            ("Liam Lawson",       30, "New Zealand",    5), ("Arvid Lindblad",   41, "United Kingdom", 5),
            ("Fernando Alonso",   14, "Spain",          6), ("Lance Stroll",     18, "Canada",         6),
            ("Pierre Gasly",      10, "France",         7), ("Franco Colapinto", 43, "Argentina",      7),
            ("Alexander Albon",   23, "Thailand",       8), ("Carlos Sainz",     55, "Spain",          8),
            ("Esteban Ocon",      31, "France",         9), ("Oliver Bearman",   87, "United Kingdom", 9),
            ("Nico Hülkenberg",   27, "Germany",       10), ("Gabriel Bortoleto", 5, "Brazil",        10),
            ("Sergio Pérez",      11, "Mexico",        11), ("Valtteri Bottas",  77, "Finland",       11),
        };
        foreach (var d in drivers)
            Exec(conn, tx,
                "INSERT INTO drivers (driver_name, driver_number, country, team_id) VALUES (@n, @num, @c, @t);",
                ("@n", d.Name), ("@num", d.Number), ("@c", d.Country), ("@t", d.TeamId));

        // ---------- grand_prix: gp_id = round_number = 1..22, спринтов 6 ----------
        var gps = new (string Name, string Circuit, string Country, bool Sprint, string Date)[]
        {
            ("Australian GP",          "Albert Park Circuit",              "Australia",      false, "2026-03-08"), //  1
            ("Chinese GP",             "Shanghai International Circuit",   "China",          true,  "2026-03-15"), //  2
            ("Japanese GP",            "Suzuka Circuit",                   "Japan",          false, "2026-03-29"), //  3
            ("Miami GP",               "Miami International Autodrome",    "United States",  true,  "2026-05-03"), //  4
            ("Canadian GP",            "Circuit Gilles Villeneuve",        "Canada",         true,  "2026-05-24"), //  5
            ("Monaco GP",              "Circuit de Monaco",                "Monaco",         false, "2026-06-07"), //  6
            ("Barcelona-Catalunya GP", "Circuit de Barcelona-Catalunya",   "Spain",          false, "2026-06-14"), //  7
            ("Austrian GP",            "Red Bull Ring",                    "Austria",        false, "2026-06-28"), //  8
            ("British GP",             "Silverstone Circuit",              "United Kingdom", true,  "2026-07-05"), //  9
            ("Belgian GP",             "Circuit de Spa-Francorchamps",     "Belgium",        false, "2026-07-19"), // 10
            ("Hungarian GP",           "Hungaroring",                      "Hungary",        false, "2026-07-26"), // 11
            ("Dutch GP",               "Circuit Zandvoort",                "Netherlands",    true,  "2026-08-23"), // 12
            ("Italian GP",             "Autodromo Nazionale Monza",        "Italy",          false, "2026-09-06"), // 13
            ("Madrid GP",              "Madring",                          "Spain",          false, "2026-09-13"), // 14
            ("Azerbaijan GP",          "Baku City Circuit",                "Azerbaijan",     false, "2026-09-26"), // 15
            ("Singapore GP",           "Marina Bay Street Circuit",        "Singapore",      true,  "2026-10-11"), // 16
            ("United States GP",       "Circuit of the Americas",          "United States",  false, "2026-10-25"), // 17
            ("Mexico City GP",         "Autódromo Hermanos Rodríguez",     "Mexico",         false, "2026-11-01"), // 18
            ("São Paulo GP",           "Autódromo José Carlos Pace",       "Brazil",         false, "2026-11-08"), // 19
            ("Las Vegas GP",           "Las Vegas Strip Circuit",          "United States",  false, "2026-11-21"), // 20
            ("Qatar GP",               "Lusail International Circuit",     "Qatar",          false, "2026-11-29"), // 21
            ("Abu Dhabi GP",           "Yas Marina Circuit",               "UAE",            false, "2026-12-06"), // 22
        };
        for (int i = 0; i < gps.Length; i++)
        {
            var g = gps[i];
            Exec(conn, tx,
                "INSERT INTO grand_prix (gp_name, circuit, country, round_number, is_sprint, race_date) " +
                "VALUES (@n, @c, @co, @r, @s, @d);",
                ("@n", g.Name), ("@c", g.Circuit), ("@co", g.Country),
                ("@r", i + 1), ("@s", g.Sprint ? 1 : 0), ("@d", g.Date));
        }

        // ---------- results: ДЕМО-данные (не реальные итоги!) ----------
        int[] racePoints   = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        int[] sprintPoints = { 8, 7, 6, 5, 4, 3, 2, 1 };

        void AddSession(int gpId, string session, int shift, int[] pointsTable)
        {
            for (int pos = 1; pos <= 22; pos++)
            {
                int driverId = ((pos - 1 + shift) % 22) + 1;
                int pts = pos <= pointsTable.Length ? pointsTable[pos - 1] : 0;
                Exec(conn, tx,
                    "INSERT INTO results (gp_id, driver_id, session_type, position, points_scored, status) " +
                    "VALUES (@gp, @dr, @st, @pos, @pts, 'Finished');",
                    ("@gp", gpId), ("@dr", driverId), ("@st", session), ("@pos", pos), ("@pts", pts));
            }
        }

        AddSession(1, "Race",   0, racePoints);
        AddSession(2, "Sprint", 5, sprintPoints);
        AddSession(2, "Race",   3, racePoints);
        AddSession(3, "Race",   7, racePoints);

        tx.Commit();
    }
}