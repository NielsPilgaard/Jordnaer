using Jordnaer.Shared;
using Jordnaer.Shared.Notifications;

namespace Jordnaer.Extensions;

public static class NotificationExtensions
{
    public static NotificationDto ToDto(this Notification notification) => new()
    {
        Id = notification.Id,
        RecipientId = notification.RecipientId,
        Title = notification.Title,
        Description = notification.Description,
        ImageUrl = notification.ImageUrl,
        LinkUrl = notification.LinkUrl,
        Type = notification.Type,
        IsRead = notification.IsRead,
        CreatedUtc = notification.CreatedUtc,
        ReadUtc = notification.ReadUtc,
        SourceType = notification.SourceType,
        SourceId = notification.SourceId
    };
}
