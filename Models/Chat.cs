using System.ComponentModel.DataAnnotations;

namespace owobot_csharp.Models;

public class Chat
{
    [Key] 
    public long Id { get; init; }
    public bool Nsfw { get; set; }
}