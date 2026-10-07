using FluentAssertions;
using Jordnaer.Shared;
using Xunit;

namespace Jordnaer.Tests.Search;

public class Utm32ConverterTests
{
	[Fact]
	public void ToWgs84_ReturnsCentralMeridian_ForFalseEasting()
	{
		var (latitude, longitude) = Utm32Converter.ToWgs84(500_000, 6_000_000);

		longitude.Should().BeApproximately(9.0, 1e-9);
		latitude.Should().BeApproximately(54.148, 0.001);
	}

	[Fact]
	public void ToWgs84_ConvertsCopenhagenAddress()
	{
		// Vestergade 12, 1456 København K, as returned by Adressevælger
		var (latitude, longitude) = Utm32Converter.ToWgs84(724468.63, 6176001.31);

		latitude.Should().BeApproximately(55.67782, 0.0001);
		longitude.Should().BeApproximately(12.57031, 0.0001);
	}
}
