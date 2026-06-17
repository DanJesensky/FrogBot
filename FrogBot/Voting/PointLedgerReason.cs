namespace FrogBot.Voting;

public enum PointLedgerReason
{
    Upvote = 0,
    UpvoteRemoved = 1,
    Downvote = 2,
    DownvoteRemoved = 3,
    Spent = 4,
    VotesCleared = 5,
    AdminAdjustment = 6,
    Migration = 7,
}
