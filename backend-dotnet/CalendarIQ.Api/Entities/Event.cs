using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalendarIQ.Api.Entities;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public long Day { get; set; }

    [Required]
    public string TimeStart { get; set; } = string.Empty;

    [Required]
    public string TimeEnd { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}