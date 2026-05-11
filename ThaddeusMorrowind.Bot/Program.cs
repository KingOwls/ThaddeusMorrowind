using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Users;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Stats;
using ThaddeusMorrowind.Bot.Features.Skills;
using ThaddeusMorrowind.Bot.Features.Inventory;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);
    string connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró ConnectionStrings:DefaultConnection.");

    builder.Services.AddDbContext<GameDbContext>(options =>
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));
    });

    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ISkillTreeService, SkillTreeService>();
builder.Services.AddScoped<ICharacterStatGrowthService, CharacterStatGrowthService>();
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents =
    GatewayIntents.Guilds |
    GatewayIntents.GuildMessages |
    GatewayIntents.MessageContent,
    LogLevel = LogSeverity.Info
}));

builder.Services.AddSingleton(serviceProvider =>
{
    DiscordSocketClient client = serviceProvider.GetRequiredService<DiscordSocketClient>();

    return new InteractionService(client.Rest, new InteractionServiceConfig
    {
        LogLevel = LogSeverity.Info,
        UseCompiledLambda = true
    });
});

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserService>());
builder.Services.AddScoped<IUserWalletService>(sp => sp.GetRequiredService<UserService>());
builder.Services.AddScoped<IUserActivityService>(sp => sp.GetRequiredService<UserService>());
builder.Services.AddScoped<IActiveCharacterService>(sp => sp.GetRequiredService<UserService>());
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<ICharacterCatalogService, CharacterCatalogService>();
builder.Services.AddSingleton<ICharacterCreationSessionStore, InMemoryCharacterCreationSessionStore>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPhase2AdvancedService, Phase2AdvancedService>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();

await host.RunAsync();
