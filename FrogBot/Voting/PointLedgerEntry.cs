using System;

namespace FrogBot.Voting;

public class PointLedgerEntry
{
    public long Id { get; set; }
    public ulong UserId { get; set; }
    public long Delta { get; set; }
    public PointLedgerReason Reason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Note { get; set; }
}
