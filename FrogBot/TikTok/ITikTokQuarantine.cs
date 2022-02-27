using Remora.Discord.API.Abstractions.Objects;

namespace FrogBot.TikTok;

public interface ITikTokQuarantineManager
{
    bool ShouldMessageBeQuarantined(IMessage message);
    IUser GetSubstituteQuarantineAuthor(IMessage message);
}