using Azure.Storage.Blobs;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Profile;

public static class ImageApi
{
    public const string ChildProfilePicturesContainerName = "childprofile-pictures";

    public static RouteGroupBuilder MapImages(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/images");

        group.RequirePerUserRateLimit();

        group.MapPut("child-profile",
            async Task<string?>
            (
                [FromBody] SetChildProfilePicture dto,
                [FromServices] JordnaerDbContext context,
                [FromServices] BlobServiceClient blobServiceClient,
                [FromServices] CurrentUser currentUser) =>
            {
                if (currentUser.Id != dto.ChildProfile.UserProfileId)
                {
                    return null;
                }

                string uri = await UploadImageAsync(blobServiceClient, dto);

                await SetChildProfilePictureAsync(context, dto, uri);

                return uri;

            }).RequireCurrentUser();

        return group;
    }

    private static async Task SetChildProfilePictureAsync(JordnaerDbContext context, SetChildProfilePicture dto, string uri)
    {
        var currentChildProfile = await context.ChildProfiles.FindAsync(dto.ChildProfile.Id);
        if (currentChildProfile is null)
        {
            dto.ChildProfile.PictureUrl = uri;
            context.ChildProfiles.Add(dto.ChildProfile);
            await context.SaveChangesAsync();
            return;
        }

        // Updating is only required if the pictureUrl is not already correct
        if (currentChildProfile.PictureUrl != uri)
        {
            currentChildProfile.PictureUrl = uri;
            context.Entry(currentChildProfile).State = EntityState.Modified;

            await context.SaveChangesAsync();
        }
    }

    private static async Task<string> UploadImageAsync(BlobServiceClient blobServiceClient, SetChildProfilePicture dto)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(ChildProfilePicturesContainerName);
        await containerClient.CreateIfNotExistsAsync();

        string blobName = dto.ChildProfile.Id.ToString("N");
        var blobClient = containerClient.GetBlobClient(blobName);

        // Convert file bytes to MemoryStream and upload
        using var memoryStream = new MemoryStream(dto.FileBytes);

        await blobClient.UploadAsync(memoryStream, overwrite: true);

        memoryStream.Close();

        return blobClient.Uri.AbsoluteUri;
    }
}

