using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace Jordnaer.Tests;

public class PageTitleTests : TestContext
{
	[Fact]
	public void AllPages_SetTitle()
	{
		// Arrange
		var routableComponents = GetRoutableComponents();
		foreach (var componentType in routableComponents)
		{
			// Act
			var component = RenderComponent<UserSearch>();

			// Assert
			component.HasComponent<MetadataComponent>().Should().BeTrue();
		}
	}

	public IEnumerable<Type> GetRoutableComponents()
	{
		// Assuming your components are in the same assembly as the entry point
		var assembly = typeof(Program).Assembly;
		var routableComponents = assembly.GetTypes()
										 .Where(t => t.GetCustomAttributes<RouteAttribute>(inherit: false).Any());

		return routableComponents;
	}
}