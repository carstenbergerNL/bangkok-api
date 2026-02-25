namespace Bangkok.Application.Dto.Auth;

/// <summary>
/// Generic success response for forgot-password (does not reveal whether email exists).
/// </summary>
public class ForgotPasswordResponse
{
    public string Message { get; set; } = "If the email exists, a recovery link has been sent.";
}
