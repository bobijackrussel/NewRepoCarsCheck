using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IOptions<DatabaseOptions> options, ILogger<DatabaseService> logger)
        {
            _connectionString = options.Value.ConnectionString ?? string.Empty;
            _logger = logger;
        }

        public async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new MySqlConnection(_connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open a database connection.");
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new MySqlCommand(query, connection);
            AddParameters(command, parameters);

            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing non-query command: {Query}", query);
                throw;
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new MySqlCommand(query, connection);
            AddParameters(command, parameters);

            try
            {
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result == null || result is DBNull)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scalar command: {Query}", query);
                throw;
            }
        }

        public async Task<IReadOnlyList<IDictionary<string, object>>> ExecuteQueryAsync(string query, IEnumerable<MySqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseService.ExecuteQueryAsync - Query: {query.Substring(0, Math.Min(100, query.Length))}...");
            var results = new List<IDictionary<string, object>>();

            await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine("Database connection opened successfully");

            await using var command = new MySqlCommand(query, connection);
            AddParameters(command, parameters);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("Query executed, reading results...");

                int rowCount = 0;
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    rowCount++;
                    var record = new Dictionary<string, object>(reader.FieldCount);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        record[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                            ? DBNull.Value
                            : reader.GetValue(i);
                    }

                    results.Add(record);
                }

                System.Diagnostics.Debug.WriteLine($"DatabaseService.ExecuteQueryAsync - Read {rowCount} rows from database");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseService.ExecuteQueryAsync - ERROR: {ex.Message}");
                _logger.LogError(ex, "Error executing query command: {Query}", query);
                throw;
            }

            System.Diagnostics.Debug.WriteLine($"DatabaseService.ExecuteQueryAsync - Returning {results.Count} results");
            return results;
        }

        private static void AddParameters(MySqlCommand command, IEnumerable<MySqlParameter>? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
        }
    }
}
