namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class StatType
{
    public ulong Id { get; set; }

    public string StatKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ValueKind { get; set; } = "flat";

    public decimal DefaultBase { get; set; }

    public bool CanBeBase { get; set; }

    public bool CanBeExtra { get; set; }

    public bool IsActive { get; set; }
}
