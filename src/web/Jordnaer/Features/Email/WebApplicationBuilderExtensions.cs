using Azure.Communication.Email;
using Jordnaer.Database;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Features.Email;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddEmailServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();
		builder.Services.AddScoped<IEmailService, EmailService>();

		var connectionString = builder.Configuration.GetConnectionString("AzureEmailService")!;

		var emailClient = new EmailClient(connectionString);
		builder.Services.AddScoped(_ => emailClient);

		return builder;
	}
}
