using System.ComponentModel.DataAnnotations;

namespace owobot_csharp.Models;

public class Chat
{
    [Key] 
    public long? ChatId { get; set; }

    public bool Nsfw { get; set; }
}