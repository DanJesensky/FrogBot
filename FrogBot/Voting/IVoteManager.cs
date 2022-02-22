using System.Collections.Generic;
using System.Threading.Tasks;

namespace FrogBot.Voting;

public interface IVoteManager
{
    Task AddVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type);

    Task RemoveVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type);

    Task RemoveAllVotesAsync(ulong channel, ulong message);

    Task<IEnumerable<Vote>> GetMessageVotesAsync(ulong channel, ulong message);

    Task<long> GetScoreAsync(ulong userId);

    Task RemoveVotesAsync(ulong channel, ulong message, VoteType type);
}