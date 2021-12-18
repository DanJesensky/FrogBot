using System.ComponentModel.DataAnnotations;

namespace FrogBot.Voting;

public class CachedUsername
{
    [Key]
    public ulong UserId { get; set; }

    public string Username { get; set; } = null!;
}