using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public interface IDatabaseService
    {
        Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default);
        Task<T?> ExecuteScalarAsync<T>(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IDictionary<string, object>>> ExecuteQueryAsync(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default);
    }
}
