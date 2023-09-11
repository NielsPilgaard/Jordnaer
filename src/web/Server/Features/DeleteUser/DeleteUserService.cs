using Jordnaer.Server.Authentication;
using Jordnaer.Server.Database;
using Jordnaer.Server.Features.Email;
using Jordnaer.Server.Features.Profile;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;

namespace Jordnaer.Server.Features.DeleteUser;

public interface IDeleteUserService
{
    Task<bool> InitiateDeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default);
    Task<bool> VerifyTokenAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default);
}

public class DeleteUserService : IDeleteUserService
{
    public const string TokenPurpose = "delete-user";
    public static readonly string TokenProvider = TokenOptions.DefaultEmailProvider;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly ISendGridClient _sendGridClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JordnaerDbContext _context;
    private readonly IDiagnosticContext _diagnosticContext;
    private readonly IImageService _imageService;

    public DeleteUserService(UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger,
        ISendGridClient sendGridClient,
        IHttpContextAccessor httpContextAccessor,
        JordnaerDbContext context,
        IDiagnosticContext diagnosticContext,
        IImageService imageService)
    {
        _userManager = userManager;
        _logger = logger;
        _sendGridClient = sendGridClient;
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _diagnosticContext = diagnosticContext;
        _imageService = imageService;
    }

    public async Task<bool> InitiateDeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _diagnosticContext.Set("userId", user.Id);

        if (_httpContextAccessor.HttpContext is null)
        {
            _logger.LogError("{httpContextAccessor} has a null HttpContext. " +
                             "A Delete User Url cannot be created.",
                nameof(IHttpContextAccessor));
            return false;
        }

        var to = new EmailAddress(user.Email);

        string token = await _userManager.GenerateUserTokenAsync(user, TokenProvider,
            TokenPurpose);

        string deletionLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/delete-user/{token}";

        string message = CreateDeleteUserEmailMessage(deletionLink);

        var email = new SendGridMessage
        {
            From = EmailConstants.ContactEmail, // Must be from a verified email
            Subject = "Anmodning om sletning af bruger",
            HtmlContent = message
        };

        email.AddTo(to);
        email.TrackingSettings = new TrackingSettings
        {
            ClickTracking = new ClickTracking { Enable = false },
            Ganalytics = new Ganalytics { Enable = false },
            OpenTracking = new OpenTracking { Enable = false },
            SubscriptionTracking = new SubscriptionTracking { Enable = false }
        };

        // TODO: Turn this email sending into an Azure Function
        var emailSentResponse = await _sendGridClient.SendEmailAsync(email, cancellationToken);

        return emailSentResponse.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default)
    {
        _diagnosticContext.Set("userId", user.Id);

        bool tokenIsValid = await _userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);
        if (tokenIsValid is false)
        {
            _logger.LogWarning("The token {token} is not valid for the token purpose {tokenPurpose}, " +
                               "stopping the deletion of the user.", token, TokenPurpose);
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // TODO: Make sure all user data is deleted here, and that owned groups are assigned new admins

            var identityResult = await _userManager.DeleteAsync(user);
            if (identityResult.Succeeded is false)
            {
                _logger.LogError("Failed to delete user. Errors: {@identityResultErrors}",
                     identityResult.Errors.Select(error => $"ErrorCode {error.Code}: {error.Description}"));

                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var childrenIds = await _context.ChildProfiles
                .Where(child => child.UserProfileId == user.Id)
                .Select(child => child.Id)
                .ToListAsync(cancellationToken);

            int modifiedRows = await _context.UserProfiles
                .Where(userProfile => userProfile.Id == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            if (modifiedRows <= 0)
            {
                _logger.LogError("Failed to delete the user profile.");

                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Delete all saved images owned by the user
            await _imageService.DeleteImageAsync(user.Id, ImageService.UserProfilePicturesContainerName, cancellationToken);
            foreach (var id in childrenIds)
            {
                await _imageService.DeleteImageAsync(id.ToString(), ImageService.ChildProfilePicturesContainerName, cancellationToken);
            }
            return true;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(exception, exception.Message);
            return false;
        }
    }

    public async Task<bool> VerifyTokenAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken = default) =>
        await _userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);


    private static string CreateDeleteUserEmailMessage(string deletionLink) =>
        $@"
<p>Hej,</p>

<p>Du har anmodet om at slette din bruger hos Mini Møder. Hvis du fortsætter, vil alle dine data blive permanent slettet og kan ikke genoprettes.</p>

<p>Hvis du er sikker på, at du vil slette din bruger, skal du klikke på linket nedenfor:</p>

<p><a href=""{deletionLink}"">Bekræft sletning af bruger</a></p>

<p>Hvis du ikke anmodede om at slette din bruger, kan du ignorere denne e-mail.</p>

<p>Med venlig hilsen,</p>

<p>Mini Møder-teamet</p>
";
}

