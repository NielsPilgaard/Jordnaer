using System.ComponentModel.DataAnnotations;
using RemindMeApp.Shared;

namespace RemindMeApp.Server.Reminders;

public enum NotificationType
{
    Email = 0,
    Push = 1,
    Sms = 2
}

public class Reminder
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    //public NotificationType NotificationType { get; set; }
    public bool IsComplete { get; set; }
    [Required]
    public DateTime ScheduleUtc { get; set; }

    [Required]
    public string OwnerId { get; set; } = default!;
}

public static class TodoMappingExtensions
{
    public static ReminderItem AsReminderItem(this Reminder reminder) =>
        new()
        {
            Id = reminder.Id,
            Title = reminder.Title,
            IsComplete = reminder.IsComplete,
        };
}
