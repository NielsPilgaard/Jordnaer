using System.Net.Http.Json;
using Jordnaer.Shared;

namespace Jordnaer.Client.Authentication;

public class AuthClient
{
    private readonly HttpClient _client;
    private readonly ILogger<AuthClient> _logger;

    public AuthClient(HttpClient client, ILogger<AuthClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _client.GetFromJsonAsync<CurrentUserDto?>("auth/current-user");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception occurred while getting current user");
            return null;
        }
    }

    public async Task<bool> LoginAsync(string? username, string? password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return false;

        var response = await _client.PostAsJsonAsync("auth/login",
            new UserInfo
            {
                Email = username,
                Password = password
            });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateUserAsync(string? username, string? password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return false;

        var response = await _client.PostAsJsonAsync("auth/register",
            new UserInfo
            {
                Email = username,
                Password = password
            });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LogoutAsync()
    {
        var response = await _client.PostAsync("auth/logout", content: null);

        return response.IsSuccessStatusCode;
    }
}
