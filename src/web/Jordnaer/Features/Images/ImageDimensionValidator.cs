namespace Jordnaer.Features.Images;

public record ImageDimensions(int Width, int Height);

public record DimensionValidationResult(bool ShowWarning, string WarningMessage);

public class ImageDimensionValidator
{
    private const int MinimumRecommendedSize = 400;
    private const int CriticalMinimumSize = 200;
    private const int MaximumRecommendedSize = 4000;
    private const double AspectRatioTolerance = 0.1;

    public DimensionValidationResult Validate(ImageDimensions dimensions)
    {
        var warnings = new List<string>();

        // Check for undersized images
        if (dimensions.Width < CriticalMinimumSize || dimensions.Height < CriticalMinimumSize)
        {
            warnings.Add($"Dit billede er {dimensions.Width}x{dimensions.Height} px, hvilket er mindre end anbefalede {MinimumRecommendedSize}x{MinimumRecommendedSize} px. Billedet kan se pixeleret ud.");
        }
        else if (dimensions.Width < MinimumRecommendedSize || dimensions.Height < MinimumRecommendedSize)
        {
            warnings.Add($"Dit billede er {dimensions.Width}x{dimensions.Height} px. For bedste kvalitet anbefaler vi mindst {MinimumRecommendedSize}x{MinimumRecommendedSize} px.");
        }

        // Check for oversized images
        if (dimensions.Width > MaximumRecommendedSize || dimensions.Height > MaximumRecommendedSize)
        {
            warnings.Add($"Dit billede er {dimensions.Width}x{dimensions.Height} px, hvilket er meget stort. Det kan tage længere tid at behandle.");
        }

        // Check aspect ratio
        if (Math.Abs(dimensions.Width - dimensions.Height) > Math.Min(dimensions.Width, dimensions.Height) * AspectRatioTolerance)
        {
            warnings.Add("Dit billede er ikke kvadratisk og vil blive beskåret.");
        }

        return new DimensionValidationResult(
            ShowWarning: warnings.Count > 0,
            WarningMessage: string.Join(" ", warnings)
        );
    }
}
