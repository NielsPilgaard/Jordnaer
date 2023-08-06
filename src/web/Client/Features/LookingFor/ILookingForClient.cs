using Refit;

namespace Jordnaer.Client.Features.LookingFor;

public interface ILookingForClient
{
    [Get("/api/looking-for")]
    Task<IApiResponse<List<Jordnaer.Shared.LookingFor>>> GetLookingFor();
}
