using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Email;

public interface IEmailClient
{
    [Post("/api/email/contact")]
    Task<IApiResponse<bool>> SendEmailFromContactForm(ContactForm contactForm);
}