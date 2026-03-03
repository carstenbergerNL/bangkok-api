using System.Text;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Dto.Tasks;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectExportService : IProjectExportService
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly IUserRepository _userRepository;
    private readonly ITaskTimeLogRepository _timeLogRepository;
    private readonly ILogger<ProjectExportService> _logger;

    public ProjectExportService(
        IProjectService projectService,
        ITaskService taskService,
        IUserRepository userRepository,
        ITaskTimeLogRepository timeLogRepository,
        ILogger<ProjectExportService> logger)
    {
        _projectService = projectService;
        _taskService = taskService;
        _userRepository = userRepository;
        _timeLogRepository = timeLogRepository;
        _logger = logger;
    }

    public async Task<(byte[]? CsvBytes, string? Error)> ExportToCsvAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var (projectResult, _) = await _projectService.GetByIdAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false);
        if (projectResult != GetProjectResult.Ok)
            return (null, projectResult == GetProjectResult.NotFound ? "Project not found." : "You do not have access to this project.");

        var tasks = await _taskService.GetByProjectIdAsync(projectId, currentUserId, null, cancellationToken).ConfigureAwait(false);
        if (tasks.Count == 0)
        {
            var csvEmpty = BuildCsv(Array.Empty<TaskResponse>(), new Dictionary<Guid, string>(), new Dictionary<Guid, decimal>());
            return (csvEmpty, null);
        }

        var taskIds = tasks.Select(t => t.Id).ToList();
        var assigneeIds = tasks.Where(t => t.AssignedToUserId.HasValue && t.AssignedToUserId.Value != Guid.Empty)
            .Select(t => t.AssignedToUserId!.Value).Distinct().ToList();
        var displayNames = assigneeIds.Count > 0
            ? await _userRepository.GetDisplayNamesByIdsAsync(assigneeIds, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();
        var loggedHoursByTask = await _timeLogRepository.GetTotalLoggedHoursByTaskIdsAsync(taskIds, cancellationToken).ConfigureAwait(false);

        var csv = BuildCsv(tasks, displayNames, loggedHoursByTask);
        _logger.LogInformation("Project export CSV generated. ProjectId: {ProjectId}, TaskCount: {Count}", projectId, tasks.Count);
        return (csv, null);
    }

    private static byte[] BuildCsv(
        IReadOnlyList<TaskResponse> tasks,
        IReadOnlyDictionary<Guid, string> assigneeDisplayNames,
        IReadOnlyDictionary<Guid, decimal> loggedHoursByTask)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Status,Priority,Assignee,Due Date,Labels,Logged Hours");

        foreach (var t in tasks)
        {
            var assignee = t.AssignedToUserId.HasValue && assigneeDisplayNames.TryGetValue(t.AssignedToUserId.Value, out var name) ? name : "";
            var dueDate = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "";
            var labels = t.Labels != null && t.Labels.Count > 0 ? string.Join("; ", t.Labels.Select(l => l.Name)) : "";
            var hours = loggedHoursByTask.TryGetValue(t.Id, out var h) ? h.ToString("F2") : "0";

            sb.Append(EscapeCsvField(t.Title));
            sb.Append(',');
            sb.Append(EscapeCsvField(t.Status));
            sb.Append(',');
            sb.Append(EscapeCsvField(t.Priority));
            sb.Append(',');
            sb.Append(EscapeCsvField(assignee));
            sb.Append(',');
            sb.Append(EscapeCsvField(dueDate));
            sb.Append(',');
            sb.Append(EscapeCsvField(labels));
            sb.Append(',');
            sb.Append(EscapeCsvField(hours));
            sb.AppendLine();
        }

        var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return utf8Bom.Concat(content).ToArray();
    }

    private static string EscapeCsvField(string? value)
    {
        if (value == null) return "\"\"";
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
