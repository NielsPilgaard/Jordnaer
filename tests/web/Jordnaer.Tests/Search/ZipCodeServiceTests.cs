using FluentAssertions;
using Jordnaer.Features.Search;
using Xunit;

namespace Jordnaer.Tests.Search;

public class ZipCodeServiceTests
{
	private readonly ZipCodeService _sut = new();

	[Fact]
	public void Search_ReturnsExactZipCodeFirst()
	{
		var result = _sut.Search("8000");

		result.Should().NotBeEmpty();
		result[0].ToString().Should().Be("8000 Aarhus C");
	}

	[Fact]
	public void Search_MatchesZipCodePrefix()
	{
		var result = _sut.Search("855");

		result.Should().Contain(zipCode => zipCode.Number == 8550);
		result.Should().OnlyContain(zipCode => zipCode.Number.ToString().StartsWith("855"));
	}

	[Fact]
	public void Search_MatchesCityName_CaseInsensitive()
	{
		var result = _sut.Search("ryomg");

		result.Should().ContainSingle().Which.ToString().Should().Be("8550 Ryomgård");
	}

	[Fact]
	public void Search_MatchesFullAutocompleteText()
	{
		var result = _sut.Search("8550 Ryomgård");

		result.Should().ContainSingle().Which.Number.Should().Be(8550);
	}

	[Fact]
	public void Search_RespectsMaxResults()
	{
		var result = _sut.Search("København", maxResults: 5);

		result.Should().HaveCount(5);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("xyzxyzxyz")]
	public void Search_ReturnsEmpty_ForNoMatch(string? query)
	{
		_sut.Search(query).Should().BeEmpty();
	}

	[Theory]
	[InlineData("8550 Ryomgård", 8550)]
	[InlineData("8550", 8550)]
	[InlineData(" 8000 Aarhus C ", 8000)]
	[InlineData("Ryomgård", 8550)]
	[InlineData("aarhus c", 8000)]
	[InlineData("Skagen", 9990)]
	public void Find_ReturnsZipCode(string text, int expectedNumber)
	{
		_sut.Find(text)!.Number.Should().Be(expectedNumber);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("0001")]
	[InlineData("xyzxyzxyz")]
	public void Find_ReturnsNull_ForUnknown(string? text)
	{
		_sut.Find(text).Should().BeNull();
	}

	[Fact]
	public void Find_ReturnsCoordinatesInDenmark()
	{
		var zipCode = _sut.Find("8000")!;

		zipCode.Latitude.Should().BeInRange(54, 58);
		zipCode.Longitude.Should().BeInRange(8, 15.5);
	}

	[Fact]
	public void FindNearest_ReturnsZipCodeContainingCoordinates()
	{
		// Aarhus Cathedral
		var zipCode = _sut.FindNearest(56.1568, 10.2107);

		zipCode!.Number.Should().Be(8000);
	}

	[Fact]
	public void FindNearest_ReturnsNull_OutsideDenmark()
	{
		// Berlin
		_sut.FindNearest(52.52, 13.405).Should().BeNull();
	}

	[Fact]
	public void AllZipCodes_AreLoaded()
	{
		// Every Danish zip code is in the 1000-9999 range, and there are just over a thousand of them
		_sut.Search("1", maxResults: int.MaxValue).Should().HaveCountGreaterThan(100);
		_sut.Find("9990").Should().NotBeNull();
		_sut.Find("1050").Should().NotBeNull();
	}
}
