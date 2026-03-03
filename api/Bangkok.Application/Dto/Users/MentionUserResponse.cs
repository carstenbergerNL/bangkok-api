namespace Bangkok.Application.Dto.Users;

/// <summary>
/// Minimal user info for @mention autocomplete.
/// </summary>
public class MentionUserResponse
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string Email { get; set; } = string.Empty;
}
