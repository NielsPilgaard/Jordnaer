using Refit;

namespace Jordnaer.Client.Features.Category;

public interface ICategoryClient
{
    [Get("/api/categories")]
    Task<IApiResponse<List<Jordnaer.Shared.Category>>> GetCategories();
}
