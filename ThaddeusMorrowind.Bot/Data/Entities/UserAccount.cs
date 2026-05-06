namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class UserAccount
{
    public ulong Id { get; set; }

    public string DiscordUserId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? BannerUrl { get; set; }

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<UserWallet> Wallets { get; set; } = new List<UserWallet>();

    public ICollection<Character> Characters { get; set; } = new List<Character>();

    public UserActiveCharacter? ActiveCharacter { get; set; }
}
