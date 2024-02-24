namespace Jordnaer.Extensions;

public static class DateTimeExtensions
{
	public static string DisplayExactTime(this DateTime date) =>
		// Example of this format: 1/9 2017 kl. 07:45
		$"afsendt {date.ToLocalTime():d/M yyyy kl. HH:mm}";

	public static string DisplayTimePassed(this DateTime date)
	{
		var timeSpan = DateTime.UtcNow.Subtract(date);

		switch (timeSpan.TotalSeconds)
		{
			case < 15:
				return "lige sendt";
			case < 60:
				return $"sendt for {Math.Floor(timeSpan.TotalSeconds)} sekunder siden";
		}

		switch (timeSpan.TotalMinutes)
		{
			case < 2:
				return $"sendt for 1 minut siden";
			case < 60:
				return $"sendt for {Math.Floor(timeSpan.TotalMinutes)} minutter siden";
		}

		switch (timeSpan.TotalHours)
		{
			case < 2:
				return $"sendt for 1 time siden";
			case < 24:
				return $"sendt for {Math.Floor(timeSpan.TotalHours)} timer siden";
		}

		switch (timeSpan.TotalDays)
		{
			case < 2:
				return "sendt igår";
			case < 7:
				return $"sendt for {Math.Floor(timeSpan.TotalDays)} dage siden";
		}

		int weeks = (int)Math.Floor(timeSpan.TotalDays / 7);

		if (weeks is 1)
		{
			return "sendt i sidste uge";
		}

		if (timeSpan.TotalDays < 365)
		{
			return $"sendt for {weeks} uger siden";
		}

		return $"afsendt {date.ToLocalTime():dd/MM/yyyy HH:mm:ss}";
	}
}
