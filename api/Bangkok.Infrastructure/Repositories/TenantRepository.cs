using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [Id], [Name], [Slug], [CreatedAt], [StripeCustomerId], [Status] FROM dbo.[Tenant] WHERE [Id] = @Id";
            return await connection.QuerySingleOrDefaultAsync<Tenant>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [Id], [Name], [Slug], [CreatedAt], [StripeCustomerId], [Status] FROM dbo.[Tenant] WHERE [Slug] = @Slug";
            return await connection.QuerySingleOrDefaultAsync<Tenant>(
                new CommandDefinition(sql, new { Slug = slug }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [Id], [Name], [Slug], [CreatedAt], [StripeCustomerId], [Status] FROM dbo.[Tenant] ORDER BY [Name]";
            var list = await connection.QueryAsync<Tenant>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "INSERT INTO dbo.[Tenant] ([Id], [Name], [Slug], [CreatedAt], [StripeCustomerId], [Status]) VALUES (@Id, @Name, @Slug, @CreatedAt, @StripeCustomerId, @Status)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { tenant.Id, tenant.Name, tenant.Slug, tenant.CreatedAt, tenant.StripeCustomerId, tenant.Status }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return tenant.Id;
        }
    }

    public async Task UpdateStripeCustomerIdAsync(Guid tenantId, string? stripeCustomerId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.[Tenant] SET [StripeCustomerId] = @StripeCustomerId WHERE [Id] = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = tenantId, StripeCustomerId = stripeCustomerId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task UpdateStatusAsync(Guid tenantId, string status, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.[Tenant] SET [Status] = @Status WHERE [Id] = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = tenantId, Status = status }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}
