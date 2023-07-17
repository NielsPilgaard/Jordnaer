using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.Infrastructure;


public sealed class GrafanaLokiOptions
{
    public const string SectionName = "GrafanaLoki";

    [Required]
    [Url]
    public string Uri { get; set; } = null!;

    [Required]
    public string Login { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public int QueueLimit { get; set; } = 500;
}
