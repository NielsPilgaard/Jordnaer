using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Jordnaer.Shared;
using Xunit;

namespace Jordnaer.Tests.Groups;

public class GroupSearchFilterTests
{
	[Fact]
	public void Validate_SearchWithoutLocation_ShouldSucceed()
	{
		// Arrange
		var filter = new GroupSearchFilter
		{
			Name = "test",
			WithinRadiusKilometers = 50 // Radius value should be ignored when no location
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert
		isValid.Should().BeTrue();
		validationResults.Should().BeEmpty();
	}

	[Fact]
	public void Validate_SearchWithLocationButNoRadius_ShouldFail()
	{
		// Arrange
		var filter = new GroupSearchFilter
		{
			Name = "test",
			Location = "Copenhagen",
			WithinRadiusKilometers = null
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert
		isValid.Should().BeFalse();
		validationResults.Should().ContainSingle()
			.Which.ErrorMessage.Should().Be("Radius skal vælges når et område er valgt.");
	}

	[Fact]
	public void Validate_SearchWithLocationAndRadius_ShouldSucceed()
	{
		// Arrange
		var filter = new GroupSearchFilter
		{
			Name = "test",
			Location = "Copenhagen",
			WithinRadiusKilometers = 50
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert
		isValid.Should().BeTrue();
		validationResults.Should().BeEmpty();
	}

	[Fact]
	public void Validate_SearchWithCoordinatesButNoRadius_ShouldFail()
	{
		// Arrange
		var filter = new GroupSearchFilter
		{
			Name = "test",
			Latitude = 55.6761,
			Longitude = 12.5683,
			WithinRadiusKilometers = null
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert
		isValid.Should().BeFalse();
		validationResults.Should().ContainSingle()
			.Which.ErrorMessage.Should().Be("Radius skal vælges når et område er valgt.");
	}

	[Fact]
	public void Validate_SearchWithCoordinatesAndRadius_ShouldSucceed()
	{
		// Arrange
		var filter = new GroupSearchFilter
		{
			Name = "test",
			Latitude = 55.6761,
			Longitude = 12.5683,
			WithinRadiusKilometers = 50
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert
		isValid.Should().BeTrue();
		validationResults.Should().BeEmpty();
	}

	[Fact]
	public void Validate_SearchWithRadiusButNoLocation_ShouldSucceed()
	{
		// Arrange - this scenario happens when user has radius slider value but clears location
		var filter = new GroupSearchFilter
		{
			Name = "test",
			WithinRadiusKilometers = 50
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert - radius is ignored when no location is provided
		isValid.Should().BeTrue();
		validationResults.Should().BeEmpty();
	}

	[Fact]
	public void Validate_SearchWithOnlyLatitudeNoLongitude_ShouldSucceedAsNoLocation()
	{
		// Arrange - incomplete coordinates should be treated as no location
		var filter = new GroupSearchFilter
		{
			Name = "test",
			Latitude = 55.6761,
			Longitude = null,
			WithinRadiusKilometers = 50
		};

		// Act
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(filter, new ValidationContext(filter), validationResults, true);

		// Assert - incomplete coordinates mean no location, so radius is ignored
		isValid.Should().BeTrue();
		validationResults.Should().BeEmpty();
	}
}
