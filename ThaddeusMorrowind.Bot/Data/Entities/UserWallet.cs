namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class UserWallet
{
    public ulong Id { get; set; }

    public ulong UserAccountId { get; set; }

    public string CurrencyKey { get; set; } = "gold";

    public long Amount { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserAccount? UserAccount { get; set; }
}
