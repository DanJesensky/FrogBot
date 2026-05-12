using System.ComponentModel;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace FrogBot.SlashCommands;

[UsedImplicitly]
public class InfoCommands(IFeedbackService feedback) : CommandGroup
{
    [Command("test")]
    [Description("Test that the bot is responding")]
    public async Task<IResult> TestAsync()
    {
        return await feedback.SendContextualSuccessAsync("✅", ct: CancellationToken);
    }

    [Command("version")]
    [Description("Show the bot version")]
    public async Task<IResult> VersionAsync() =>
        await feedback.SendContextualSuccessAsync(
            typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "Version unset",
            ct: CancellationToken);
}
