namespace F1Tracker.Models;

/// <param name="Tab">Ключ вкладки в DataWindow: teams / drivers / grand_prix</param>
public record SearchHit(string Tab, string Kind, string Title, string Subtitle);