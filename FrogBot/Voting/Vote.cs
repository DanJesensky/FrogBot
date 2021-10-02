namespace FrogBot.Voting
{
    public class Vote
    {
        public ulong VoterId { get; set; }

        public ulong ReceiverId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public VoteType VoteType { get; set; }
    }
}