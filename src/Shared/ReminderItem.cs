/* Shared classes can be referenced by both the Client and Server */

using System.ComponentModel.DataAnnotations;

namespace RemindMeApp.Shared;

public class ReminderItem
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = default!;

    public string? Description { get; set; }

    //public NotificationType NotificationType { get; set; }

    public bool IsComplete { get; set; }

    [Required]
    public DateTime ScheduleUtc { get; set; }
}
