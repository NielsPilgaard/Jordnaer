using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemindMeApp.Server.Authentication;
using RemindMeApp.Server.Data;
using RemindMeApp.Server.Extensions;
using RemindMeApp.Shared;

namespace RemindMeApp.Server.Reminders;

internal static class RemindMeApi
{
    public static RouteGroupBuilder MapReminders(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/reminders");

        group.RequireAuthorization();

        group.RequirePerUserRateLimit();

        group.WithTags("Reminders");

        group.MapGet("", async ([FromServices] RemindMeDbContext db, HttpContext context) =>
            await db.Reminders.Where(reminder =>
                    reminder.OwnerId == context.GetUserId())
                .Select(reminder => reminder.AsReminderItem())
                .AsNoTracking()
                .ToListAsync());

        group.MapGet("{id:int}",
            async Task<Results<Ok<ReminderItem>, NotFound>> ([FromServices] RemindMeDbContext db,
                [FromRoute] int id,
                HttpContext context) =>
        {
            return await db.Reminders.FindAsync(id) switch
            {
                { } reminder when reminder.OwnerId == context.GetUserId() || context.UserIsAdmin() => TypedResults.Ok(reminder.AsReminderItem()),
                _ => TypedResults.NotFound()
            };
        });

        group.MapPost("/",
            async Task<Created<ReminderItem>> ([FromServices] RemindMeDbContext db,
                [FromBody] ReminderItem newReminder,
                HttpContext context) =>
        {
            var reminder = new Reminder
            {
                Title = newReminder.Title,
                OwnerId = context.GetUserId()
            };

            db.Reminders.Add(reminder);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/reminders/{reminder.Id}", reminder.AsReminderItem());
        });

        group.MapPut("/{id:int}",
            async Task<Results<Ok, NotFound, BadRequest>> ([FromServices] RemindMeDbContext db,
                [FromRoute] int id,
                [FromBody] ReminderItem reminderItem,
                HttpContext context) =>
        {
            if (id != reminderItem.Id)
            {
                return TypedResults.BadRequest();
            }

            int rowsAffected = await db.Reminders.Where(reminder =>
                    reminder.Id == id && (reminder.OwnerId == context.GetUserId() || context.UserIsAdmin())).ExecuteUpdateAsync(updates =>
                                                updates.SetProperty(t => t.IsComplete, reminderItem.IsComplete)
                                                       .SetProperty(t => t.Title, reminderItem.Title));

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });

        group.MapDelete("/{id:int}",
            async Task<Results<NotFound, Ok>> ([FromServices] RemindMeDbContext db,
                [FromRoute] int id,
                HttpContext context) =>
        {
            int rowsAffected = await db.Reminders.Where(reminder => reminder.Id == id && (reminder.OwnerId == context.GetUserId() || context.UserIsAdmin()))
                                             .ExecuteDeleteAsync();

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });

        return group;
    }
}
