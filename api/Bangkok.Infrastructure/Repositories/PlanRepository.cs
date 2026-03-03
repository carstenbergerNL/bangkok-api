using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PlanRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [Id], [Name], [PriceMonthly], [PriceYearly], [MaxProjects], [MaxUsers], [AutomationEnabled], [CreatedAt], [StripePriceIdMonthly], [StripePriceIdYearly], [StorageLimitMB] FROM dbo.[Plan] WHERE [Id] = @Id";
            return await connection.QuerySingleOrDefaultAsync<Plan>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [Id], [Name], [PriceMonthly], [PriceYearly], [MaxProjects], [MaxUsers], [AutomationEnabled], [CreatedAt], [StripePriceIdMonthly], [StripePriceIdYearly], [StorageLimitMB] FROM dbo.[Plan] ORDER BY [PriceMonthly]";
            var list = await connection.QueryAsync<Plan>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Plan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT TOP 1 [Id], [Name], [PriceMonthly], [PriceYearly], [MaxProjects], [MaxUsers], [AutomationEnabled], [CreatedAt], [StripePriceIdMonthly], [StripePriceIdYearly], [StorageLimitMB] FROM dbo.[Plan] WHERE [StripePriceIdMonthly] = @PriceId OR [StripePriceIdYearly] = @PriceId";
            return await connection.QuerySingleOrDefaultAsync<Plan>(
                new CommandDefinition(sql, new { PriceId = stripePriceId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}
