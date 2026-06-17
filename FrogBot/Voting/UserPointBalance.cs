using System.ComponentModel.DataAnnotations;

namespace FrogBot.Voting;

public class UserPointBalance
{
    [Key]
    public ulong UserId { get; set; }
    public long Balance { get; set; }
}
