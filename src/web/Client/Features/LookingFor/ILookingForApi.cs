using Refit;

namespace Jordnaer.Client.Features.LookingFor;

public interface ILookingForApi
{
    [Get("/api/looking-for")]
    Task<IApiResponse<List<Jordnaer.Shared.LookingFor>>> GetLookingFor();
}
