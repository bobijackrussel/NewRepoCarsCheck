using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public class ViolationService : IViolationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ViolationService> _logger;

        public ViolationService(IDatabaseService databaseService, ILogger<ViolationService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<bool> ReportAsync(ViolationReport report, CancellationToken cancellationToken = default)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            const string command = @"INSERT INTO violation_reports (reservation_id, user_id, vtype, severity, description, status)
                                     VALUES (@reservationId, @userId, @type, @severity, @description, @status)";

            var parameters = new List<MySqlParameter>
            {
                new("@reservationId", report.ReservationId),
                new("@userId", report.UserId),
                new("@type", EnumToDatabase(report.Type)),
                new("@severity", EnumToDatabase(report.Severity)),
                new("@description", string.IsNullOrWhiteSpace(report.Description) ? DBNull.Value : report.Description!),
                new("@status", EnumToDatabase(report.Status))
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to report violation for reservation {ReservationId}", report.ReservationId);
                throw;
            }
        }

        public async Task<IReadOnlyList<ViolationReport>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, reservation_id, user_id, vtype, severity, description, status, reported_at, resolved_at, resolution_notes
                                  FROM violation_reports
                                  ORDER BY reported_at DESC";

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
                return rows.Select(MapViolation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load violation reports");
                throw;
            }
        }

        public async Task<IReadOnlyList<ViolationReport>> GetByReservationAsync(long reservationId, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, reservation_id, user_id, vtype, severity, description, status, reported_at, resolved_at, resolution_notes
                                  FROM violation_reports
                                  WHERE reservation_id = @reservationId";

            var parameters = new List<MySqlParameter>
            {
                new("@reservationId", reservationId)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapViolation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load violation reports for reservation {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<IReadOnlyList<ViolationReport>> GetByUserAsync(long userId, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, reservation_id, user_id, vtype, severity, description, status, reported_at, resolved_at, resolution_notes
                                  FROM violation_reports
                                  WHERE user_id = @userId
                                  ORDER BY reported_at DESC";

            var parameters = new List<MySqlParameter>
            {
                new("@userId", userId)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapViolation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load violation reports for user {UserId}", userId);
                throw;
            }
        }

        private static ViolationReport MapViolation(IDictionary<string, object> record)
        {
            var report = new ViolationReport();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                report.Id = Convert.ToInt64(idObj);
            }

            if (record.TryGetValue("reservation_id", out var reservationObj) && reservationObj is not DBNull)
            {
                report.ReservationId = Convert.ToInt64(reservationObj);
            }

            if (record.TryGetValue("user_id", out var userObj) && userObj is not DBNull)
            {
                report.UserId = Convert.ToInt64(userObj);
            }

            if (record.TryGetValue("vtype", out var typeObj) && typeObj is not DBNull)
            {
                report.Type = ParseEnum<ViolationType>(Convert.ToString(typeObj));
            }

            if (record.TryGetValue("severity", out var severityObj) && severityObj is not DBNull)
            {
                report.Severity = ParseEnum<ViolationSeverity>(Convert.ToString(severityObj));
            }

            if (record.TryGetValue("description", out var descriptionObj) && descriptionObj is not DBNull)
            {
                report.Description = Convert.ToString(descriptionObj);
            }

            if (record.TryGetValue("status", out var statusObj) && statusObj is not DBNull)
            {
                report.Status = ParseEnum<ViolationStatus>(Convert.ToString(statusObj));
            }

            if (record.TryGetValue("reported_at", out var reportedObj) && reportedObj is not DBNull)
            {
                report.ReportedAt = Convert.ToDateTime(reportedObj);
            }

            if (record.TryGetValue("resolved_at", out var resolvedObj) && resolvedObj is not DBNull)
            {
                report.ResolvedAt = Convert.ToDateTime(resolvedObj);
            }

            if (record.TryGetValue("resolution_notes", out var notesObj) && notesObj is not DBNull)
            {
                report.ResolutionNotes = Convert.ToString(notesObj);
            }

            return report;
        }

        private static TEnum ParseEnum<TEnum>(string? value) where TEnum : struct, Enum
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            return default;
        }

        private static string EnumToDatabase<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return value.ToString().ToUpperInvariant();
        }
    }
}
