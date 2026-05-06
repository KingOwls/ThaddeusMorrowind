namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class ExperienceCurve
{
    public ulong Id { get; set; }

    public string CurveKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal BaseXp { get; set; }

    public decimal Exponent { get; set; }

    public uint MaxLevel { get; set; } = 100;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ExperienceLevel> Levels { get; set; } = new List<ExperienceLevel>();
}
