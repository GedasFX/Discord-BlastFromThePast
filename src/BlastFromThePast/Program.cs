using BlastFromThePast;
using BlastFromThePast.Data;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var discord = new DiscordSocketClient(new DiscordSocketConfig()
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});

var serviceProvider = new ServiceCollection()
    .AddSingleton(discord)
    .AddDbContext<AppDbContext>()
    .AddScoped<ImageImporter>()
    .AddHttpClient()
    .BuildServiceProvider();

var service = new InteractionService(discord, new InteractionServiceConfig { DefaultRunMode = RunMode.Async });
await service.AddModulesAsync(typeof(Program).Assembly, serviceProvider);

service.Log += Log;
discord.Log += Log;

discord.Ready += async () =>
{
#if DEBUG
    await service.RegisterCommandsToGuildAsync(896045547641798667);
#else
    await service.RegisterCommandsGloballyAsync();
#endif
};

service.InteractionExecuted += async (_, context, result) =>
{
    if (result.IsSuccess)
        return;

    if (context.Interaction.HasResponded)
        await context.Interaction.FollowupAsync($"Fail: {result.Error} - {result.ErrorReason}",
            ephemeral: true);
    else
        await context.Interaction.RespondAsync($"Fail: {result.Error} - {result.ErrorReason}",
            ephemeral: true);
};

discord.InteractionCreated += async x =>
{
    var ctx = new SocketInteractionContext(discord, x);
    await service.ExecuteCommandAsync(ctx, serviceProvider);
};

using (var scope = serviceProvider.CreateScope())
{
    Directory.CreateDirectory("./data");
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await ctx.Database.MigrateAsync();
}


await discord.LoginAsync(TokenType.Bot, BotConfig.Instance.Token);
await discord.StartAsync();

await Task.Delay(-1);
return;

async Task Log(LogMessage message)
{
    await Console.Out.WriteLineAsync(message.Exception == null
        ? $"{message.Severity:G} {message.Message}"
        : $"{message.Severity:G} {message.Message}\n{message.Exception}");
}