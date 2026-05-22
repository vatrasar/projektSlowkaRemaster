using System;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Reverse { get; set; }
    public DateTime CreatedAt { get; set; }
}
