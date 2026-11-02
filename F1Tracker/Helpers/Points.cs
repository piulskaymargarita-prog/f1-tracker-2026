namespace F1Tracker.Helpers;

public static class Points
{
    private static readonly int[] RaceTable = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
    private static readonly int[] SprintTable = { 8, 7, 6, 5, 4, 3, 2, 1 };

    /// <summary>Очки за позицию. Qualifying очков не даёт.</summary>
    public static int For(string sessionType, int position)
    {
        int[] table = sessionType switch
        {
            "Race" => RaceTable,
            "Sprint" => SprintTable,
            _ => Array.Empty<int>()
        };
        return position >= 1 && position <= table.Length ? table[position - 1] : 0;
    }
}