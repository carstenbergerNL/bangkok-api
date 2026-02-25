using System.Data;

namespace Bangkok.Infrastructure.Data;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
