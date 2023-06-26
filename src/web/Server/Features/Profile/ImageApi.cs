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

                string uri = await UploadImageAsync(blobServiceClient, dto.ChildProfile.Id.ToString("N"), dto.FileBytes);

                await SetChildProfilePictureAsync(context, dto, uri);

                return uri;

            }).RequireCurrentUser();


        group.MapPut("user-profile",
            async Task<string?>
            (
                [FromBody] SetUserProfilePicture dto,
                [FromServices] JordnaerDbContext context,
                [FromServices] BlobServiceClient blobServiceClient,
                [FromServices] CurrentUser currentUser) =>
            {
                if (currentUser.Id != dto.UserProfile.Id)
                {
                    return null;
                }

                string uri = await UploadImageAsync(blobServiceClient, dto.UserProfile.Id, dto.FileBytes);

                await SetUserProfilePictureAsync(context, dto, uri);

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


    private static async Task SetUserProfilePictureAsync(JordnaerDbContext context, SetUserProfilePicture dto, string uri)
    {
        var currentUserProfile = await context.UserProfiles.FindAsync(dto.UserProfile.Id);
        if (currentUserProfile is null)
        {
            dto.UserProfile.ProfilePictureUrl = uri;
            context.UserProfiles.Add(dto.UserProfile);
            await context.SaveChangesAsync();
            return;
        }

        // Updating is only required if the pictureUrl is not already correct
        if (currentUserProfile.ProfilePictureUrl != uri)
        {
            currentUserProfile.ProfilePictureUrl = uri;
            context.Entry(currentUserProfile).State = EntityState.Modified;

            await context.SaveChangesAsync();
        }
    }

    private static async Task<string> UploadImageAsync(BlobServiceClient blobServiceClient, string blobName, byte[] fileBytes)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(ChildProfilePicturesContainerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);

        // Convert file bytes to MemoryStream and upload
        using var memoryStream = new MemoryStream(fileBytes);

        await blobClient.UploadAsync(memoryStream, overwrite: true);

        memoryStream.Close();

        return blobClient.Uri.AbsoluteUri;
    }
}

