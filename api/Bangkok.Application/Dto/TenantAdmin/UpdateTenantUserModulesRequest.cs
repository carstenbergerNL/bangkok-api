namespace Bangkok.Application.Dto.TenantAdmin;

/// <summary>
/// Replace user's module access with the given list. Only active tenant modules are applied.
/// </summary>
public class UpdateTenantUserModulesRequest
{
    public IReadOnlyList<string> ModuleKeys { get; set; } = Array.Empty<string>();
}
