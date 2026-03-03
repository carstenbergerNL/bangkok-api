namespace Bangkok.Domain;

/// <summary>
/// Value of a custom field on a task. Value stored as string (NVARCHAR(MAX)).
/// </summary>
public class TaskCustomFieldValue
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid FieldId { get; set; }
    public string? Value { get; set; }
}
