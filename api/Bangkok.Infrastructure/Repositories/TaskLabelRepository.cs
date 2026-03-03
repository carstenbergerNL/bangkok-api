using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskLabelRepository : ITaskLabelRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskLabelRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Guid>> GetLabelIdsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT LabelId FROM dbo.TaskLabel WHERE TaskId = @TaskId";
            var ids = await connection.QueryAsync<Guid>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return ids.ToList();
        }
    }

    public async Task SetForTaskAsync(Guid taskId, IReadOnlyList<Guid> labelIds, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskLabel WHERE TaskId = @TaskId", new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (labelIds != null && labelIds.Count > 0)
            {
                foreach (var labelId in labelIds.Distinct())
                {
                    if (labelId == Guid.Empty) continue;
                    await connection.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO dbo.TaskLabel (Id, TaskId, LabelId) VALUES (@Id, @TaskId, @LabelId)",
                        new { Id = Guid.NewGuid(), TaskId = taskId, LabelId = labelId },
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }
        }
    }
}
