using Refit;

namespace Jordnaer.Features.Category;

public interface ICategoryClient
{
	[Get("/api/categories")]
	Task<IApiResponse<List<Jordnaer.Shared.Category>>> GetCategories();
}
