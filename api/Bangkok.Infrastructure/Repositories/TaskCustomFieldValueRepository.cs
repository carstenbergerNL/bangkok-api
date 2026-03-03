using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskCustomFieldValueRepository : ITaskCustomFieldValueRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskCustomFieldValueRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TaskCustomFieldValue>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TaskId, FieldId, Value FROM dbo.TaskCustomFieldValue WHERE TaskId = @TaskId";
            var list = await connection.QueryAsync<TaskCustomFieldValue>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Dictionary<Guid, IReadOnlyList<TaskCustomFieldValue>>> GetByTaskIdsAsync(IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<TaskCustomFieldValue>>();
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var sql = "SELECT Id, TaskId, FieldId, Value FROM dbo.TaskCustomFieldValue WHERE TaskId IN @TaskIds";
            var list = (await connection.QueryAsync<TaskCustomFieldValue>(
                new CommandDefinition(sql, new { TaskIds = taskIds }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            return list.GroupBy(x => x.TaskId).ToDictionary(g => g.Key, g => (IReadOnlyList<TaskCustomFieldValue>)g.ToList());
        }
    }

    public async Task SetForTaskAsync(Guid taskId, IReadOnlyList<(Guid FieldId, string? Value)> values, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM dbo.TaskCustomFieldValue WHERE TaskId = @TaskId", new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            foreach (var (fieldId, value) in values)
            {
                var id = Guid.NewGuid();
                await connection.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO dbo.TaskCustomFieldValue (Id, TaskId, FieldId, Value) VALUES (@Id, @TaskId, @FieldId, @Value)",
                    new { Id = id, TaskId = taskId, FieldId = fieldId, Value = value },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
        }
    }
}
