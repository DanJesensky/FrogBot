using System.ComponentModel.DataAnnotations;

namespace FrogBot.Voting;

public class BannedVoter
{
    [Key]
    public ulong UserId { get; set; }
}