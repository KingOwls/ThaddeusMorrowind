using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ThaddeusMorrowind.Bot;

public class Worker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;
    private readonly ILogger<Worker> _logger;

    private ulong? _guildId;

    public Worker(
        DiscordSocketClient client,
        InteractionService interactions,
        IConfiguration configuration,
        IServiceProvider services,
        ILogger<Worker> logger)
    {
        _client = client;
        _interactions = interactions;
        _configuration = configuration;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += LogDiscordMessageAsync;
        _interactions.Log += LogDiscordMessageAsync;

        _client.Ready += OnClientReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;

        string? token = _configuration["Discord:Token"];
        string? guildIdText = _configuration["Discord:GuildId"];

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "No se encontró el token del bot. Configura Discord:Token usando dotnet user-secrets.");
        }

        if (ulong.TryParse(guildIdText, out ulong parsedGuildId))
        {
            _guildId = parsedGuildId;
        }

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation("Thaddeus Morrowind está iniciando...");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Esto pasa cuando detenemos el bot con Ctrl + C.
        }
    }

    private async Task OnClientReadyAsync()
    {
        if (_guildId.HasValue)
        {
            await _interactions.RegisterCommandsToGuildAsync(_guildId.Value);
            _logger.LogInformation("Comandos registrados en el servidor de pruebas: {GuildId}", _guildId.Value);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Comandos registrados globalmente.");
        }

        await _client.SetGameAsync("Mini MMORPG en construcción ⚔️");
        _logger.LogInformation("Thaddeus Morrowind está conectado como {BotName}", _client.CurrentUser.Username);
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            SocketInteractionContext context = new(_client, interaction);

            IResult result = await _interactions.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Error ejecutando comando: {Error} - {Reason}",
                    result.Error,
                    result.ErrorReason);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ocurrió un error procesando una interacción de Discord.");
        }
    }

    private Task LogDiscordMessageAsync(LogMessage message)
    {
        LogLevel level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(level, message.Exception, "[Discord] {Message}", message.Message);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Apagando Thaddeus Morrowind...");

        await _client.StopAsync();

        await base.StopAsync(cancellationToken);
    }
}