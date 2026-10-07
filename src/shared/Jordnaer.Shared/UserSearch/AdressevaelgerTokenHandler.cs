using Microsoft.Extensions.Options;

namespace Jordnaer.Shared;

/// <summary>
/// Appends the mandatory <c>token</c> query parameter to every Adressevælger request.
/// </summary>
public class AdressevaelgerTokenHandler(IOptions<AdressevaelgerOptions> options) : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri is not null)
		{
			var builder = new UriBuilder(request.RequestUri);
			var token = $"token={Uri.EscapeDataString(options.Value.Token)}";
			builder.Query = string.IsNullOrEmpty(builder.Query)
				? token
				: $"{builder.Query.TrimStart('?')}&{token}";

			request.RequestUri = builder.Uri;
		}

		return base.SendAsync(request, cancellationToken);
	}
}
