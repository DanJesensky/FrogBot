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

            {
                await using var db = services.GetRequiredService<VoteDbContext>();
                await db.Database.EnsureCreatedAsync();
                await db.Database.MigrateAsync();
            }

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
            services.AddLogging(logging =>
                logging.AddConsole().SetMinimumLevel(LogLevel.Trace));

            services.AddDbContext<VoteDbContext>(opt =>
                opt.UseMySql(hostContext.Configuration[ConfigurationKeys.ConnectionString], MariaDbServerVersion.LatestSupportedServerVersion));

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
                .AddChatCommand<TopChatCommand>()
                .AddChatCommand<PointsChatCommand>();
        }
    }
}