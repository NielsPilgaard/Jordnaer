namespace RemindMeApp.Server.Authentication;

public class EmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}

public interface IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage);
}
