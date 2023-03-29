using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace owobot_csharp.Models;

public class Chat
{
    [Key] 
    public long Id { get; init; }

    [DefaultValue(false)]
    public bool Nsfw { get; set; } = false;
}