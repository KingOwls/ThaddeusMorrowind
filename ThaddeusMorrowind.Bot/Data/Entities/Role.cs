namespace ThaddeusMorrowind.Bot.Data.Entities;

public sealed class Role
{
    public ulong Id { get; set; }

    public string RoleKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? IconUrl { get; set; }

    public string? BannerUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public uint DisplayOrder { get; set; } = 999;
}
