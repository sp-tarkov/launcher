namespace SPTarkov.Core.Models;

public record ForgeResponseMessage
{
    // TODO: these need to be more consistent on the API
    public const string InvalidEmail = "The email field must be a valid email address.";
    public const string Success = "success";
    public const string Authenticated = "authenticated";
    public const string InvalidCredentials = "PASSWORD_LOGIN_UNAVAILABLE";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string ValidationFailed = "VALIDATION_FAILED";
}
