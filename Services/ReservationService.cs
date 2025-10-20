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
    public class ReservationService : IReservationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(IDatabaseService databaseService, ILogger<ReservationService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Reservation>> GetUserReservationsAsync(long userId, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, user_id, vehicle_id, start_date, end_date, status, total_amount, notes,
                                         created_at, updated_at, cancelled_at, cancellation_reason
                                  FROM reservations
                                  WHERE user_id = @userId
                                  ORDER BY start_date DESC";

            var parameters = new List<MySqlParameter>
            {
                new("@userId", userId)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapReservation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load reservations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, user_id, vehicle_id, start_date, end_date, status, total_amount, notes,
                                         created_at, updated_at, cancelled_at, cancellation_reason
                                  FROM reservations";

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
                return rows.Select(MapReservation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load reservations");
                throw;
            }
        }

        public async Task<bool> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(nameof(reservation));
            }

            if (!await IsVehicleAvailableAsync(reservation.VehicleId, reservation.StartDate, reservation.EndDate, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The vehicle is not available for the selected dates.");
            }

            const string command = @"INSERT INTO reservations (user_id, vehicle_id, start_date, end_date, status, total_amount, notes)
                                     VALUES (@userId, @vehicleId, @startDate, @endDate, @status, @totalAmount, @notes)";

            var parameters = new List<MySqlParameter>
            {
                new("@userId", reservation.UserId),
                new("@vehicleId", reservation.VehicleId),
                new("@startDate", reservation.StartDate),
                new("@endDate", reservation.EndDate),
                new("@status", EnumToDatabase(reservation.Status)),
                new("@totalAmount", reservation.TotalAmount),
                new("@notes", string.IsNullOrWhiteSpace(reservation.Notes) ? DBNull.Value : reservation.Notes!)
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create reservation for user {UserId}", reservation.UserId);
                throw;
            }
        }

        public async Task<bool> IsVehicleAvailableAsync(long vehicleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT COUNT(1)
                                  FROM reservations
                                  WHERE vehicle_id = @vehicleId
                                    AND status IN ('PENDING', 'CONFIRMED')
                                    AND start_date < @endDate
                                    AND end_date > @startDate";

            var parameters = new List<MySqlParameter>
            {
                new("@vehicleId", vehicleId),
                new("@startDate", startDate),
                new("@endDate", endDate)
            };

            try
            {
                var conflicts = await _databaseService.ExecuteScalarAsync<long?>(query, parameters, cancellationToken).ConfigureAwait(false);
                return conflicts.GetValueOrDefault() == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check availability for vehicle {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<bool> CancelAsync(long reservationId, string? reason = null, CancellationToken cancellationToken = default)
        {
            const string command = @"UPDATE reservations
                                     SET status = 'CANCELLED',
                                         cancelled_at = @cancelledAt,
                                         cancellation_reason = @reason
                                     WHERE id = @id";

            var parameters = new List<MySqlParameter>
            {
                new("@cancelledAt", DateTime.UtcNow),
                new("@reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason!),
                new("@id", reservationId)
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel reservation {ReservationId}", reservationId);
                throw;
            }
        }

        private static Reservation MapReservation(IDictionary<string, object> record)
        {
            var reservation = new Reservation();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                reservation.Id = Convert.ToInt64(idObj);
            }

            if (record.TryGetValue("user_id", out var userObj) && userObj is not DBNull)
            {
                reservation.UserId = Convert.ToInt64(userObj);
            }

            if (record.TryGetValue("vehicle_id", out var vehicleObj) && vehicleObj is not DBNull)
            {
                reservation.VehicleId = Convert.ToInt64(vehicleObj);
            }

            if (record.TryGetValue("start_date", out var startObj) && startObj is not DBNull)
            {
                reservation.StartDate = Convert.ToDateTime(startObj);
            }

            if (record.TryGetValue("end_date", out var endObj) && endObj is not DBNull)
            {
                reservation.EndDate = Convert.ToDateTime(endObj);
            }

            if (record.TryGetValue("status", out var statusObj) && statusObj is not DBNull)
            {
                reservation.Status = ParseEnum<ReservationStatus>(Convert.ToString(statusObj));
            }

            if (record.TryGetValue("total_amount", out var amountObj) && amountObj is not DBNull)
            {
                reservation.TotalAmount = Convert.ToDecimal(amountObj);
            }

            if (record.TryGetValue("notes", out var notesObj) && notesObj is not DBNull)
            {
                reservation.Notes = Convert.ToString(notesObj);
            }

            if (record.TryGetValue("created_at", out var createdObj) && createdObj is not DBNull)
            {
                reservation.CreatedAt = Convert.ToDateTime(createdObj);
            }

            if (record.TryGetValue("updated_at", out var updatedObj) && updatedObj is not DBNull)
            {
                reservation.UpdatedAt = Convert.ToDateTime(updatedObj);
            }

            if (record.TryGetValue("cancelled_at", out var cancelledObj) && cancelledObj is not DBNull)
            {
                reservation.CancelledAt = Convert.ToDateTime(cancelledObj);
            }

            if (record.TryGetValue("cancellation_reason", out var reasonObj) && reasonObj is not DBNull)
            {
                reservation.CancellationReason = Convert.ToString(reasonObj);
            }

            return reservation;
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
