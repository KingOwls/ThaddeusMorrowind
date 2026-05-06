namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class CharacterStat
{
    public ulong Id { get; set; }

    public ulong CharacterId { get; set; }

    public ulong StatTypeId { get; set; }

    public decimal BaseValue { get; set; }

    public decimal ExtraValue { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Character? Character { get; set; }

    public StatType? StatType { get; set; }

    public decimal TotalValue => BaseValue + ExtraValue;
}
