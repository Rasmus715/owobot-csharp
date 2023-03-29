using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace owobot_csharp.Models;

public class User
{
    [Key] 
    public long Id { get; init; }

    [DefaultValue(false)]
    public bool Nsfw { get; set; } = false;

    [DefaultValue("en-US")]
    public string Language { get; set; } = "en-US";
}