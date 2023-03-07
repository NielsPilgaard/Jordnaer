using System.Net;
using System.Net.Http.Json;
using RemindMeApp.Shared;

namespace RemindMeApp.Client;

public class ReminderClient
{
    private readonly HttpClient _client;
    private readonly ILogger<ReminderClient> _logger;

    public ReminderClient(HttpClient client, ILogger<ReminderClient> logger)
    {
        _client = client;
        _logger = logger;
    }
    public async Task<ReminderItem?> AddReminderAsync(string? title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        ReminderItem? createdTodo = null;

        var response = await _client.PostAsJsonAsync("reminders", new ReminderItem { Title = title });

        if (response.IsSuccessStatusCode)
        {
            createdTodo = await response.Content.ReadFromJsonAsync<ReminderItem>();
        }

        return createdTodo;
    }

    public async Task<bool> DeleteReminderAsync(int id)
    {
        var response = await _client.DeleteAsync($"reminders/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<(HttpStatusCode, List<ReminderItem>?)> GetRemindersAsync()
    {
        var response = await _client.GetAsync("reminders");
        var statusCode = response.StatusCode;
        List<ReminderItem>? reminders = null;

        if (response.IsSuccessStatusCode)
        {
            reminders = await response.Content.ReadFromJsonAsync<List<ReminderItem>>();
        }

        return (statusCode, reminders);
    }
}
