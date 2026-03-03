namespace Bangkok.Application.Dto.Projects;

public class ProjectDashboardResponse
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal TotalEstimatedHours { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public int OverBudgetTaskCount { get; set; }
    public IReadOnlyList<TasksPerStatusItem> TasksPerStatus { get; set; } = Array.Empty<TasksPerStatusItem>();
    public IReadOnlyList<TasksPerMemberItem> TasksPerMember { get; set; } = Array.Empty<TasksPerMemberItem>();
}

public class TasksPerStatusItem
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TasksPerMemberItem
{
    public Guid UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public int Count { get; set; }
}
