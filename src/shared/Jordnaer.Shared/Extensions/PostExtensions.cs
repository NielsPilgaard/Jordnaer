namespace Jordnaer.Shared;

public static class PostExtensions
{
	public static PostDto ToPostDto(this Post post)
	{
		return new PostDto
		{
			Id = post.Id,
			Text = post.Text,
			CreatedUtc = post.CreatedUtc,
			Author = new UserSlim
			{
				Id = post.UserProfileId,
				ProfilePictureUrl = post.UserProfile.ProfilePictureUrl,
				UserName = post.UserProfile.UserName,
				DisplayName = post.UserProfile.DisplayName
			},
			City = post.City,
			ZipCode = post.ZipCode,
			Categories = post.Categories.Select(category => category.Name).ToList()
		};
	}

	public static GroupPostDto ToGroupPostDto(this GroupPost post)
	{
		return new GroupPostDto
		{
			Id = post.Id,
			Text = post.Text,
			CreatedUtc = post.CreatedUtc,
			Author = new UserSlim
			{
				Id = post.UserProfileId,
				ProfilePictureUrl = post.UserProfile.ProfilePictureUrl,
				UserName = post.UserProfile.UserName,
				DisplayName = post.UserProfile.DisplayName
			}
		};
	}
}