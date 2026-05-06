namespace ThaddeusMorrowind.Bot.Features.Characters.Creation;

public sealed class CharacterCreationSession
{
    public ulong UserId { get; init; }

    public ulong ChannelId { get; init; }

    public string Name { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;

    public string? PortraitUrl { get; set; }

    public ulong? NationId { get; set; }

    public ulong? ProfessionId { get; set; }

    public ulong? RoleId { get; set; }

    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;

    public void Refresh()
    {
        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
    }
}
