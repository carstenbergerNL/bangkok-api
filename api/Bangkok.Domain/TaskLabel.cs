namespace Bangkok.Domain;

/// <summary>
/// Task-Label junction. Links a Task to a Label.
/// </summary>
public class TaskLabel
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid LabelId { get; set; }
}
