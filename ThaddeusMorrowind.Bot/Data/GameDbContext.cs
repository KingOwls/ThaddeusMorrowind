using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data.Entities;

namespace ThaddeusMorrowind.Bot.Data;

public sealed class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<UserWallet> UserWallets => Set<UserWallet>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<UserActiveCharacter> UserActiveCharacters => Set<UserActiveCharacter>();
    public DbSet<CharacterStat> CharacterStats => Set<CharacterStat>();
    public DbSet<Nation> Nations => Set<Nation>();
    public DbSet<Profession> Professions => Set<Profession>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<StatType> StatTypes => Set<StatType>();
    public DbSet<ExperienceCurve> ExperienceCurves => Set<ExperienceCurve>();
    public DbSet<ExperienceLevel> ExperienceLevels => Set<ExperienceLevel>();
    public DbSet<CharacterExperienceLog> CharacterExperienceLogs => Set<CharacterExperienceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DiscordUserId).HasColumnName("discord_user_id");
            entity.Property(x => x.Username).HasColumnName("username");
            entity.Property(x => x.DisplayName).HasColumnName("display_name");
            entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(x => x.BannerUrl).HasColumnName("banner_url");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.DiscordUserId).IsUnique();
        });

        modelBuilder.Entity<UserWallet>(entity =>
        {
            entity.ToTable("user_wallets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            entity.Property(x => x.CurrencyKey).HasColumnName("currency_key");
            entity.Property(x => x.Amount).HasColumnName("amount");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.UserAccount)
                .WithMany(x => x.Wallets)
                .HasForeignKey(x => x.UserAccountId);
        });

        modelBuilder.Entity<Nation>(entity =>
        {
            entity.ToTable("nations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.NationKey).HasColumnName("nation_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ShortDescription).HasColumnName("short_description");
            entity.Property(x => x.IconUrl).HasColumnName("icon_url");
            entity.Property(x => x.BannerUrl).HasColumnName("banner_url");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.DisplayOrder).HasColumnName("display_order");
        });

        modelBuilder.Entity<Profession>(entity =>
        {
            entity.ToTable("professions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProfessionKey).HasColumnName("profession_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ShortDescription).HasColumnName("short_description");
            entity.Property(x => x.IconUrl).HasColumnName("icon_url");
            entity.Property(x => x.BannerUrl).HasColumnName("banner_url");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.DisplayOrder).HasColumnName("display_order");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.RoleKey).HasColumnName("role_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.ShortDescription).HasColumnName("short_description");
            entity.Property(x => x.IconUrl).HasColumnName("icon_url");
            entity.Property(x => x.BannerUrl).HasColumnName("banner_url");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.DisplayOrder).HasColumnName("display_order");
        });

        modelBuilder.Entity<StatType>(entity =>
        {
            entity.ToTable("stat_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.StatKey).HasColumnName("stat_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Category).HasColumnName("category");
            entity.Property(x => x.ValueKind).HasColumnName("value_kind");
            entity.Property(x => x.DefaultBase).HasColumnName("default_base");
            entity.Property(x => x.CanBeBase).HasColumnName("can_be_base");
            entity.Property(x => x.CanBeExtra).HasColumnName("can_be_extra");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.ToTable("characters");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.Nickname).HasColumnName("nickname");
            entity.Property(x => x.PortraitUrl).HasColumnName("portrait_url");
            entity.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(x => x.BannerUrl).HasColumnName("banner_url");
            entity.Property(x => x.ProfileColorHex).HasColumnName("profile_color_hex");
            entity.Property(x => x.NationId).HasColumnName("nation_id");
            entity.Property(x => x.ProfessionId).HasColumnName("profession_id");
            entity.Property(x => x.RoleId).HasColumnName("role_id");
            entity.Property(x => x.Level).HasColumnName("level");
            entity.Property(x => x.Experience).HasColumnName("experience");
            entity.Property(x => x.IsMainCharacter).HasColumnName("is_main_character");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            entity.Property(x => x.DeletedByDiscordUserId).HasColumnName("deleted_by_discord_user_id");
            entity.Property(x => x.DeleteReason).HasColumnName("delete_reason");

            entity.HasOne(x => x.UserAccount)
                .WithMany(x => x.Characters)
                .HasForeignKey(x => x.UserAccountId);

            entity.HasOne(x => x.Nation)
                .WithMany()
                .HasForeignKey(x => x.NationId);

            entity.HasOne(x => x.Profession)
                .WithMany()
                .HasForeignKey(x => x.ProfessionId);

            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<UserActiveCharacter>(entity =>
        {
            entity.ToTable("user_active_characters");
            entity.HasKey(x => x.UserAccountId);
            entity.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            entity.Property(x => x.CharacterId).HasColumnName("character_id");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.UserAccount)
                .WithOne(x => x.ActiveCharacter)
                .HasForeignKey<UserActiveCharacter>(x => x.UserAccountId);

            entity.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId);
        });

        modelBuilder.Entity<CharacterStat>(entity =>
        {
            entity.ToTable("character_stats");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CharacterId).HasColumnName("character_id");
            entity.Property(x => x.StatTypeId).HasColumnName("stat_type_id");
            entity.Property(x => x.BaseValue).HasColumnName("base_value");
            entity.Property(x => x.ExtraValue).HasColumnName("extra_value");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.Character)
                .WithMany(x => x.Stats)
                .HasForeignKey(x => x.CharacterId);

            entity.HasOne(x => x.StatType)
                .WithMany()
                .HasForeignKey(x => x.StatTypeId);
        });

        modelBuilder.Entity<ExperienceCurve>(entity =>
        {
            entity.ToTable("experience_curves");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CurveKey).HasColumnName("curve_key");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.BaseXp).HasColumnName("base_xp");
            entity.Property(x => x.Exponent).HasColumnName("exponent");
            entity.Property(x => x.MaxLevel).HasColumnName("max_level");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.CurveKey).IsUnique();
        });

        modelBuilder.Entity<ExperienceLevel>(entity =>
        {
            entity.ToTable("experience_levels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ExperienceCurveId).HasColumnName("experience_curve_id");
            entity.Property(x => x.Level).HasColumnName("level");
            entity.Property(x => x.TotalXpRequired).HasColumnName("total_xp_required");
            entity.Property(x => x.XpToNextLevel).HasColumnName("xp_to_next_level");

            entity.HasOne(x => x.ExperienceCurve)
                .WithMany(x => x.Levels)
                .HasForeignKey(x => x.ExperienceCurveId);
        });

        modelBuilder.Entity<CharacterExperienceLog>(entity =>
        {
            entity.ToTable("character_experience_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CharacterId).HasColumnName("character_id");
            entity.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            entity.Property(x => x.ActorDiscordUserId).HasColumnName("actor_discord_user_id");
            entity.Property(x => x.SourceKey).HasColumnName("source_key");
            entity.Property(x => x.DeltaExperience).HasColumnName("delta_experience");
            entity.Property(x => x.PreviousExperience).HasColumnName("previous_experience");
            entity.Property(x => x.NewExperience).HasColumnName("new_experience");
            entity.Property(x => x.PreviousLevel).HasColumnName("previous_level");
            entity.Property(x => x.NewLevel).HasColumnName("new_level");
            entity.Property(x => x.Reason).HasColumnName("reason");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId);

            entity.HasOne(x => x.UserAccount)
                .WithMany()
                .HasForeignKey(x => x.UserAccountId);
        });
    }
}
