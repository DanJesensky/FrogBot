using System.ComponentModel.DataAnnotations;

namespace FrogBot.Voting;

public class CachedUsername
{
    [Key]
    public ulong UserId { get; set; }

    [MaxLength(40)]
    public string Username { get; set; } = null!;
}