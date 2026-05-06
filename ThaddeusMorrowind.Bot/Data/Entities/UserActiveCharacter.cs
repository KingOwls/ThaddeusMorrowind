namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class UserActiveCharacter
{
    public ulong UserAccountId { get; set; }

    public ulong CharacterId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserAccount? UserAccount { get; set; }

    public Character? Character { get; set; }
}
