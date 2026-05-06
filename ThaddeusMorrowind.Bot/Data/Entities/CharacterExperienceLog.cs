namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class CharacterExperienceLog
{
    public ulong Id { get; set; }

    public ulong CharacterId { get; set; }

    public ulong? UserAccountId { get; set; }

    public string? ActorDiscordUserId { get; set; }

    public string SourceKey { get; set; } = "manual_admin";

    public long DeltaExperience { get; set; }

    public ulong PreviousExperience { get; set; }

    public ulong NewExperience { get; set; }

    public uint PreviousLevel { get; set; }

    public uint NewLevel { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public Character? Character { get; set; }

    public UserAccount? UserAccount { get; set; }
}
