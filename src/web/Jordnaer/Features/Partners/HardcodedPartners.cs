
using Jordnaer.Shared;

namespace Jordnaer.Features.Partners;

public static class HardcodedPartners
{
    public static readonly List<Partner> Partners =
    [
        new Partner
        {
            Name = string.Empty,
            Description = "Moon Creative laver illustrationer og grafisk design til iværksættere og virksomheder, der ønsker at skabe forandring i mennesker",
            LogoUrl = "https://usercontent.one/wp/www.mooncreative.dk/wp-content/uploads/2022/04/Logo_mooncreative_long-2-e1649063478985.png",
            Link = "https://www.mooncreative.dk/",
            UserId = string.Empty
        },
        new Partner
        {
            Name = string.Empty,
            Description = "Microsoft for Startups samler mennesker, viden og teknologi for at hjælpe iværksættere på alle stadier med at løse udfordringer i startfasen.",
            LogoUrl = "https://i.ibb.co/v4Q7pkQ/startups-wordmark-purple.png",
            Link = "https://www.microsoft.com/en-us/startups",
            UserId = string.Empty
        },
        new Partner
        {
            Name = string.Empty,
            Description = "Tak til elmah.io for deres støtte! Deres fejlhåndteringsværktøj hjælper med at sikre en problemfri brugeroplevelse i vores applikationer.",
            LogoUrl = "/images/partners/elmah-io-logo.png",
            Link = "https://elmah.io/",
            UserId = string.Empty
        }
    ];

}