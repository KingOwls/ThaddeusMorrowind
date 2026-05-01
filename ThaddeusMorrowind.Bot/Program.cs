using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
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

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();

host.Run();