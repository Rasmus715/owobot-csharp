using System.ComponentModel.DataAnnotations;

namespace owobot_csharp.Models;

public class User
{
    [Key] 
    public long? Id { get; set; }
    public bool Nsfw { get; set; }
    public string? Language { get; set; }
}