namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class ExperienceLevel
{
    public ulong Id { get; set; }

    public ulong ExperienceCurveId { get; set; }

    public uint Level { get; set; }

    public ulong TotalXpRequired { get; set; }

    public ulong XpToNextLevel { get; set; }

    public ExperienceCurve? ExperienceCurve { get; set; }
}
