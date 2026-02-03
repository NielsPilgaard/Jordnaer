namespace Jordnaer.Features.Images;

public static class ProfilePictureConstants
{
    public const int CroppedImageSize = 200;
    public const int SmallAvatarSize = 40;
    public const int ProfileCardSize = 150;
    public const int FullProfileSize = 250;

    public record PreviewSize(string Label, int Size, string ElementIdSuffix)
    {
        public string GetElementId(string componentId) => $"preview-{ElementIdSuffix}-{componentId}";
    }

    public static readonly PreviewSize[] PreviewSizes =
    [
        new PreviewSize("Profilkort", ProfileCardSize, "150"),
        new PreviewSize("Lille avatar", SmallAvatarSize, "40"),
        new PreviewSize("Fuld profil", FullProfileSize, "250")
    ];
}
