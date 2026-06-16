using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalendarIQ.Api.Entities;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public long Day { get; set; }

    [Required]
    public string TimeStart { get; set; } = string.Empty;

    [Required]
    public string TimeEnd { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Location { get; set; }

    public bool Locked { get; set; } = false;

    public bool ReminderEnabled { get; set; } = false;

    public int ReminderTime { get; set; } = 15;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}