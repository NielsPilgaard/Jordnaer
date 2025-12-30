using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;

namespace Jordnaer.Extensions;

public static class ServiceCollectionExtensions
{
	internal const string ServiceName = "Jordnaer";

	public static IServiceCollection AddAzureBlobStorageDataProtection(this IServiceCollection services)
	{
		const string containerName = "data-protection";
		const string fileName = "keys.xml";

		services.AddDataProtection(options => options.ApplicationDiscriminator = ServiceName)
				.PersistKeysToAzureBlobStorage(static provider =>
				{
					var containerClient = provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(containerName);

					try
					{
						containerClient.CreateIfNotExists();
					}
					catch (Exception ex)
					{
						var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DataProtection");
#pragma warning disable CA1873 // Avoid potentially expensive logging
						logger.LogError(ex, "Failed to create or access data protection container '{Container}'", containerName);
#pragma warning restore CA1873 // Avoid potentially expensive logging
						throw;
					}

					return containerClient.GetBlobClient(fileName);
				});

		return services;
	}
}