using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.TenantAdmin;

/// <summary>
/// Invite or add user to tenant. If user does not exist, a new user is created (invite flow).
/// </summary>
public class InviteTenantUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Tenant role: Admin or Member.</summary>
    public string TenantRole { get; set; } = "Member";

    /// <summary>Module keys to grant access to. Only active tenant modules are applied.</summary>
    public IReadOnlyList<string>? ModuleKeys { get; set; }
}
