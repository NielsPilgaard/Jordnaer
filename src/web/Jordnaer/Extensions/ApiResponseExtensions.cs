using Jordnaer.Models;
using MudBlazor;
using Refit;
using System.Net;

namespace Jordnaer.Extensions;

public static class ApiResponseExtensions
{
    public static async Task<TResponse?> NotifyUserOfResponseAsync<TResponse>(
        this Task<IApiResponse<TResponse>> apiCall,
        ISnackbar snackbar)
    {
        var response = await apiCall;
        switch (response.StatusCode)
        {
            case { } when response is { IsSuccessStatusCode: true, Content: not null }:
                return response.Content!;

            case HttpStatusCode.TooManyRequests:
                snackbar.Add(ErrorMessages.High_Load, Severity.Info);
                break;

            default:
                snackbar.Add(ErrorMessages.Something_Went_Wrong_Refresh, Severity.Warning);
                break;
        }

        return default;
    }
}