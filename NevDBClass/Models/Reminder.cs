using System.ComponentModel.DataAnnotations;

namespace NevDBClass.Models;

public class Reminder
{
    public enum ReminderType
    {
        // Stored as int. Do not reorder.
        Anniversary,
        Birthday,
        Consecration,
        Death
    }

    [Key] 
    public int Id { get; set; }

    [Required] 
    public string Name { get; set; } = string.Empty;

    [Required] 
    public ReminderType Type { get; set; }

    [Required] 
    public DateOnly Date { get; set; }
    
    public string? Notes { get; set; } = "";
    public string? Mah { get; set; } = "";
    public string? Roj { get; set; } = "";
    
}