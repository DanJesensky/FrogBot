using System.Threading;
using System.Threading.Tasks;
using FrogBot.ChatCommands;
using FrogBot.Responders;
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Rest.Extensions;

namespace FrogBot {
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var services = host.Services;

            try
            {
                await using var db = services.GetRequiredService<VoteDbContext>();
                await db.Database.EnsureCreatedAsync();
                await db.Database.MigrateAsync();
            }
            catch { }

            var client = services.GetRequiredService<DiscordGatewayClient>();
            var cancellationSource = new CancellationTokenSource();
            await client.RunAsync(cancellationSource.Token);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configure => configure
                    .AddCommandLine(args)
                    .AddEnvironmentVariables())
                .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

            services.AddDbContext<VoteDbContext>(opt =>
                opt.UseNpgsql(hostContext.Configuration[ConfigurationKeys.ConnectionString]));

            services.AddTransient<IVoteManager, VoteManager>();
            services.AddTransient<IVoteEmojiProvider, VoteEmojiProvider>();

            services.AddDiscordGateway(sp => sp.GetRequiredService<IConfiguration>()[ConfigurationKeys.Token])
                .AddDiscordRest(sp => sp.GetRequiredService<IConfiguration>()[ConfigurationKeys.Token])
                .AddDiscordApi()
                .Configure<DiscordGatewayClientOptions>(opt =>
                {
                    opt.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.GuildMessageTyping;
                })
                .AddResponder<ChatCommandResponder>()
                .AddResponder<MessageVoteCreationResponder>()
                .AddResponder<VoteAddResponder>()
                .AddResponder<VoteRemoveResponder>()
                .AddResponder<RemoveAllVotesResponder>()
                .AddResponder<DeleteMessageResponder>()
                .AddChatCommand<TestChatCommand>()
                .AddChatCommand<SayCommand>()
                .AddChatCommand<VersionCommand>()
                .AddChatCommand<TopChatCommand>()
                .AddChatCommand<PointsChatCommand>()
                .AddChatCommand<EmojiIdCommand>();
        }
    }
}