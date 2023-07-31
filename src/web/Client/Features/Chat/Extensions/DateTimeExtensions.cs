namespace Jordnaer.Client.Features.Chat.Extensions;

public static class DateTimeExtensions
{
    public static string GetTimePassed(this DateTime date)
    {
        var timeSpan = DateTime.UtcNow.Subtract(date);

        if (timeSpan.TotalSeconds < 60)
            return "lige sendt";

        if (timeSpan.TotalMinutes < 60)
            return $"{Math.Floor(timeSpan.TotalMinutes)} minutter siden";

        if (timeSpan.TotalHours < 24)
            return $"{Math.Floor(timeSpan.TotalHours)} timer siden";
        if (timeSpan.TotalDays < 7)
            return $"{Math.Floor(timeSpan.TotalDays)} dage siden";

        int weeks = (int)Math.Floor(timeSpan.TotalDays / 7);

        if (weeks is >= 1 and < 2)
            return "sidste uge";

        if (timeSpan.TotalDays < 365)
            return $"{weeks} uger siden";

        return $"afsendt {date.ToLocalTime():g}";
    }
}
