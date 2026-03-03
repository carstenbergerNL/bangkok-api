namespace Bangkok.Domain;

/// <summary>
/// Global catalog of available SaaS modules (Project Management, CRM, Analytics, etc.).
/// </summary>
public class Module
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
}
