namespace Bangkok.Application.Dto.Modules;

/// <summary>
/// Module with active flag for a tenant (admin management).
/// </summary>
public class TenantModuleResponse
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
