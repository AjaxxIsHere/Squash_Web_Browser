using System.ComponentModel.DataAnnotations;

namespace Squash_Web_Browser.Models;

public class Setting
{
    [Key]
    public string? Key { get; set; }
    public string? Value { get; set; }
}
