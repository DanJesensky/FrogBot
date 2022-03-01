using System.Diagnostics.CodeAnalysis;

namespace FrogBot;

[ExcludeFromCodeCoverage]
public class FrogBotOptions
{
    public string Token { get; set; } = null!;
    public ulong ServerId { get; set; }
    public ulong BotUserId { get; set; }
}