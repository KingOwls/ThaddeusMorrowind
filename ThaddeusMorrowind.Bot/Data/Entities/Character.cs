namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class Character
{
    public ulong Id { get; set; }

    public ulong UserAccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public string? PortraitUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? BannerUrl { get; set; }

    public string? ProfileColorHex { get; set; }

    public ulong NationId { get; set; }

    public ulong ProfessionId { get; set; }

    public ulong RoleId { get; set; }

    public uint Level { get; set; } = 1;

    public ulong Experience { get; set; }

    public bool IsMainCharacter { get; set; }

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByDiscordUserId { get; set; }

    public string? DeleteReason { get; set; }

    public UserAccount? UserAccount { get; set; }

    public Nation? Nation { get; set; }

    public Profession? Profession { get; set; }

    public Role? Role { get; set; }

    public ICollection<CharacterStat> Stats { get; set; } = new List<CharacterStat>();
}
