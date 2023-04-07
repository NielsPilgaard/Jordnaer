namespace Jordnaer.Client;

public class UserClient
{
    private readonly HttpClient _client;

    public UserClient(HttpClient client)
    {
        _client = client;
    }
    public async Task<bool> DeleteUserAsync()
    {
        var response = await _client.DeleteAsync("api/user");
        return response.IsSuccessStatusCode;
    }
}
