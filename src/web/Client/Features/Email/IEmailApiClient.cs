using Jordnaer.Shared.Email;
using Refit;

namespace Jordnaer.Client.Features.Email;

public interface IEmailApiClient
{
    [Post("/api/email/contact")]
    Task<IApiResponse<bool>> SendEmailFromContactForm(ContactForm contactForm);
}
