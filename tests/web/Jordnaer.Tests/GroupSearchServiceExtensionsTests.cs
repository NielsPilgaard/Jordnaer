using FluentAssertions;
using Jordnaer.Shared;
using Xunit;

namespace Jordnaer.Server.Features.GroupSearch.Tests;


[Trait("Category", "UnitTest")]
public class GroupSearchServiceExtensionsTests
{
    [Fact]
    public void ApplyNameFilter_WithNullName_ReturnsOriginalGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();

        // Act
        var result = groups.ApplyNameFilter(null);

        // Assert
        result.Should().BeEquivalentTo(groups);
    }

    [Fact]
    public void ApplyNameFilter_WithEmptyName_ReturnsOriginalGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();

        // Act
        var result = groups.ApplyNameFilter("");

        // Assert
        result.Should().BeEquivalentTo(groups);
    }

    [Fact]
    public void ApplyNameFilter_WithValidName_ReturnsFilteredGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();

        // Act
        var result = groups.ApplyNameFilter("2");

        // Assert
        result.Should().ContainSingle().Which.Name.Should().Be("Group 2");
    }

    [Fact]
    public void ApplyCategoryFilter_WithNullCategories_ReturnsOriginalGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();

        // Act
        var result = groups.ApplyCategoryFilter(null);

        // Assert
        result.Should().BeEquivalentTo(groups);
    }

    [Fact]
    public void ApplyCategoryFilter_WithEmptyCategories_ReturnsOriginalGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();

        // Act
        var result = groups.ApplyCategoryFilter(Array.Empty<string>());

        // Assert
        result.Should().BeEquivalentTo(groups);
    }

    [Fact]
    public void ApplyCategoryFilter_WithValidCategory_ReturnsFilteredGroup()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();
        string[] categories = new[] { "Category 1" };

        // Act
        var result = groups.ApplyCategoryFilter(categories);

        // Assert
        result.Should().ContainSingle()
              .Which.Categories.Should().ContainSingle()
              .Which.Name.Should().Be("Category 1");
    }

    [Fact]
    public void ApplyCategoryFilter_WithValidCategories_ReturnsFilteredGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();
        string[] categories = new[] { "Category 1", "Category 2" };

        // Act
        var result = groups.ApplyCategoryFilter(categories);

        // Assert
        result.Should().Contain(groups.ElementAt(0)).And.Contain(groups.ElementAt(1));
    }

    [Fact]
    public void ApplyCategoryFilter_WithInvalidCategory_ReturnsNoGroups()
    {
        // Arrange
        var groups = CreateGroups().AsQueryable();
        string[] categories = new[] { "Category" };

        // Act
        var result = groups.ApplyCategoryFilter(categories);

        // Assert
        result.Should().BeEmpty();
    }

    // Helper method to create a list of groups with the required properties
    private static Group[] CreateGroups() =>
        new[]
        {
            new Group
            {
                Name = "Group 1",
                ShortDescription = "A short description for Group 1",
                Categories = new List<Shared.Category> {new() {Name = "Category 1"}}
            },
            new Group
            {
                Name = "Group 2",
                ShortDescription = "A short description for Group 2",
                Categories = new List<Shared.Category> {new() {Name = "Category 2"}}
            },
            new Group
            {
                Name = "Group 3",
                ShortDescription = "A short description for Group 3",
                Categories = new List<Shared.Category> {new() {Name = "Category 3"}}
            }
        };
}