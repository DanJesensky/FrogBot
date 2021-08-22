using System.Threading.Tasks;

namespace FrogBot.Voting
{
    public interface IVoteManager
    {
        Task AddVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type);
        
        Task RemoveVoteAsync(ulong channel, ulong message, ulong author, ulong voter, VoteType type);

        Task RemoveAllVotesAsync(ulong channel, ulong message);
    }
}