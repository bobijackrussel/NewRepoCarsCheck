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

        private readonly List<Reservation> _fallbackReservations = new();
        private readonly object _syncRoot = new();
        private long _nextReservationId = 1;
        private bool _useFallbackStore;

        public ReservationService(IDatabaseService databaseService, ILogger<ReservationService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Reservation>> GetUserReservationsAsync(long userId, CancellationToken cancellationToken = default)
        {
            if (_useFallbackStore)
            {
                return GetFallbackReservations(userId);
            }

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
            catch (Exception ex) when (ShouldUseFallback(ex))
            {
                EnableFallback(ex);
                return GetFallbackReservations(userId);
            }
        }

        public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (_useFallbackStore)
            {
                return GetFallbackReservations();
            }

            const string query = @"SELECT id, user_id, vehicle_id, start_date, end_date, status, total_amount, notes,
                                         created_at, updated_at, cancelled_at, cancellation_reason
                                  FROM reservations";

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
                return rows.Select(MapReservation).ToList();
            }
            catch (Exception ex) when (ShouldUseFallback(ex))
            {
                EnableFallback(ex);
                return GetFallbackReservations();
            }
        }

        public async Task<bool> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(nameof(reservation));
            }

            if (_useFallbackStore)
            {
                return CreateFallbackReservation(reservation);
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
                if (!ShouldUseFallback(ex))
                {
                    _logger.LogWarning(ex, "Database reservation creation failed. Falling back to in-memory store.");
                }

                EnableFallback(ex);
                return CreateFallbackReservation(reservation);
            }
        }

        public async Task<bool> IsVehicleAvailableAsync(long vehicleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            if (_useFallbackStore)
            {
                return IsVehicleAvailableFallback(vehicleId, startDate, endDate);
            }

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
            catch (Exception ex) when (ShouldUseFallback(ex))
            {
                EnableFallback(ex);
                return IsVehicleAvailableFallback(vehicleId, startDate, endDate);
            }
        }

        public async Task<bool> CancelAsync(long reservationId, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (_useFallbackStore)
            {
                return CancelFallbackReservation(reservationId, reason);
            }

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
            catch (Exception ex) when (ShouldUseFallback(ex))
            {
                EnableFallback(ex);
                return CancelFallbackReservation(reservationId, reason);
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

        private IReadOnlyList<Reservation> GetFallbackReservations(long? userId = null)
        {
            lock (_syncRoot)
            {
                IEnumerable<Reservation> query = _fallbackReservations;

                if (userId.HasValue)
                {
                    query = query.Where(r => r.UserId == userId.Value);
                }

                return query.Select(CloneReservation).ToList();
            }
        }

        private bool CreateFallbackReservation(Reservation reservation)
        {
            lock (_syncRoot)
            {
                if (!IsVehicleAvailableFallback(reservation.VehicleId, reservation.StartDate, reservation.EndDate))
                {
                    throw new InvalidOperationException("The vehicle is not available for the selected dates.");
                }

                var stored = CloneReservation(reservation);
                stored.Id = _nextReservationId++;
                var now = DateTime.UtcNow;
                stored.CreatedAt = now;
                stored.UpdatedAt = now;

                _fallbackReservations.Add(stored);

                reservation.Id = stored.Id;
                reservation.CreatedAt = stored.CreatedAt;
                reservation.UpdatedAt = stored.UpdatedAt;

                return true;
            }
        }

        private bool IsVehicleAvailableFallback(long vehicleId, DateTime startDate, DateTime endDate)
        {
            lock (_syncRoot)
            {
                return !_fallbackReservations.Any(r => r.VehicleId == vehicleId
                    && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                    && r.StartDate < endDate
                    && r.EndDate > startDate);
            }
        }

        private bool CancelFallbackReservation(long reservationId, string? reason)
        {
            lock (_syncRoot)
            {
                var reservation = _fallbackReservations.FirstOrDefault(r => r.Id == reservationId);
                if (reservation == null)
                {
                    return false;
                }

                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancellationReason = reason;
                reservation.CancelledAt = DateTime.UtcNow;
                reservation.UpdatedAt = reservation.CancelledAt.Value;
                return true;
            }
        }

        private static Reservation CloneReservation(Reservation source)
        {
            return new Reservation
            {
                Id = source.Id,
                UserId = source.UserId,
                VehicleId = source.VehicleId,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TotalAmount = source.TotalAmount,
                Notes = source.Notes,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt,
                CancelledAt = source.CancelledAt,
                CancellationReason = source.CancellationReason
            };
        }

        private bool ShouldUseFallback(Exception ex)
        {
            return ex is MySqlException
                   || ex.InnerException is MySqlException
                   || ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase);
        }

        private void EnableFallback(Exception ex)
        {
            if (_useFallbackStore)
            {
                return;
            }

            _useFallbackStore = true;
            _logger.LogWarning(ex, "Falling back to in-memory reservation store.");
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
