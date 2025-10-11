using System;

namespace Squash_Web_Browser.Models;

public class History
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public DateTime Timestamp { get; set; }
}
