using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantSubscriptionRepository : ITenantSubscriptionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantSubscriptionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantSubscription?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT TOP 1 [Id], [TenantId], [PlanId], [Status], [StartDate], [EndDate], [StripeSubscriptionId]
                FROM dbo.[TenantSubscription]
                WHERE [TenantId] = @TenantId
                  AND [Status] IN (N'Active', N'Trial')
                  AND ([EndDate] IS NULL OR [EndDate] > GETUTCDATE())
                ORDER BY [StartDate] DESC";
            return await connection.QuerySingleOrDefaultAsync<TenantSubscription>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<TenantSubscription?> GetCurrentByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT TOP 1 [Id], [TenantId], [PlanId], [Status], [StartDate], [EndDate], [StripeSubscriptionId] FROM dbo.[TenantSubscription] WHERE [TenantId] = @TenantId ORDER BY [StartDate] DESC";
            return await connection.QuerySingleOrDefaultAsync<TenantSubscription>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<TenantSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT TOP 1 [Id], [TenantId], [PlanId], [Status], [StartDate], [EndDate], [StripeSubscriptionId] FROM dbo.[TenantSubscription] WHERE [StripeSubscriptionId] = @StripeSubscriptionId";
            return await connection.QuerySingleOrDefaultAsync<TenantSubscription>(
                new CommandDefinition(sql, new { StripeSubscriptionId = stripeSubscriptionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<TenantSubscription> CreateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "INSERT INTO dbo.[TenantSubscription] ([Id], [TenantId], [PlanId], [Status], [StartDate], [EndDate], [StripeSubscriptionId]) VALUES (@Id, @TenantId, @PlanId, @Status, @StartDate, @EndDate, @StripeSubscriptionId)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { subscription.Id, subscription.TenantId, subscription.PlanId, subscription.Status, subscription.StartDate, subscription.EndDate, subscription.StripeSubscriptionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return subscription;
        }
    }

    public async Task UpdateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.[TenantSubscription] SET [PlanId] = @PlanId, [Status] = @Status, [StartDate] = @StartDate, [EndDate] = @EndDate, [StripeSubscriptionId] = @StripeSubscriptionId WHERE [Id] = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { subscription.Id, subscription.PlanId, subscription.Status, subscription.StartDate, subscription.EndDate, subscription.StripeSubscriptionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<(int ActiveCount, int TrialCount, int ChurnedCount, decimal Mrr)> GetSubscriptionStatsAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
SELECT
    SUM(CASE WHEN ts.[Status] = N'Active' AND (ts.[EndDate] IS NULL OR ts.[EndDate] > GETUTCDATE()) THEN 1 ELSE 0 END) AS ActiveCount,
    SUM(CASE WHEN ts.[Status] = N'Trial' AND (ts.[EndDate] IS NULL OR ts.[EndDate] > GETUTCDATE()) THEN 1 ELSE 0 END) AS TrialCount,
    SUM(CASE WHEN ts.[Status] = N'Cancelled' OR ts.[EndDate] < GETUTCDATE() THEN 1 ELSE 0 END) AS ChurnedCount,
    ISNULL(SUM(CASE WHEN ts.[Status] = N'Active' AND (ts.[EndDate] IS NULL OR ts.[EndDate] > GETUTCDATE()) THEN ISNULL(p.[PriceMonthly], 0) ELSE 0 END), 0) AS Mrr
FROM dbo.[TenantSubscription] ts
INNER JOIN dbo.[Plan] p ON ts.[PlanId] = p.[Id]";
            var row = await connection.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (row == null)
                return (0, 0, 0, 0);
            int active = (int)(row.ActiveCount ?? 0);
            int trial = (int)(row.TrialCount ?? 0);
            int churned = (int)(row.ChurnedCount ?? 0);
            decimal mrr = (decimal)(row.Mrr ?? 0m);
            return (active, trial, churned, mrr);
        }
    }

    public async Task<IReadOnlyList<(Guid TenantId, string PlanName, string SubscriptionStatus)>> GetCurrentSubscriptionWithPlanForAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
WITH [Ranked] AS (
    SELECT [TenantId], [PlanId], [Status],
        ROW_NUMBER() OVER (PARTITION BY [TenantId] ORDER BY [StartDate] DESC) AS [Rn]
    FROM dbo.[TenantSubscription]
)
SELECT r.[TenantId], p.[Name] AS [PlanName], r.[Status] AS [SubscriptionStatus]
FROM [Ranked] r
INNER JOIN dbo.[Plan] p ON r.[PlanId] = p.[Id]
WHERE r.[Rn] = 1";
            var rows = await connection.QueryAsync<(Guid TenantId, string PlanName, string SubscriptionStatus)>(
                new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return rows.ToList();
        }
    }
}
