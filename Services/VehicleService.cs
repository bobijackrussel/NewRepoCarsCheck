using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public class VehicleService : IVehicleService
    {
        private const string AllVehiclesCacheKey = "VehicleService_AllVehicles";
        private static readonly TimeSpan VehicleCacheDuration = TimeSpan.FromMinutes(5);

        private readonly IDatabaseService _databaseService;
        private readonly ILogger<VehicleService> _logger;
        private readonly IMemoryCache _cache;

        public VehicleService(IDatabaseService databaseService, ILogger<VehicleService> logger, IMemoryCache cache)
        {
            _databaseService = databaseService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine("=== VehicleService.GetAllAsync called ===");
            _logger.LogInformation("GetAllAsync called - checking cache");

            if (_cache.TryGetValue(AllVehiclesCacheKey, out IReadOnlyList<Vehicle>? cachedVehicles))
            {
                System.Diagnostics.Debug.WriteLine($"Returning {cachedVehicles.Count} vehicles from cache");
                _logger.LogInformation("Returning {Count} vehicles from cache", cachedVehicles.Count);
                return cachedVehicles;
            }

            const string query = @"SELECT id, branch_id, vin, plate_number, make, model, model_year, category, transmission, fuel,
                                         seats, doors, color, description, daily_rate, status, created_at, updated_at
                                  FROM vehicles";

            try
            {
                System.Diagnostics.Debug.WriteLine("Executing database query to fetch vehicles");
                _logger.LogInformation("Executing database query to fetch vehicles");
                var rows = await _databaseService.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Database returned {rows.Count} rows");
                _logger.LogInformation("Database returned {RowCount} rows", rows.Count);

                var vehicles = rows.Select(MapVehicle).ToList();
                System.Diagnostics.Debug.WriteLine($"Mapped {vehicles.Count} vehicles from database");
                _logger.LogInformation("Mapped {VehicleCount} vehicles from database", vehicles.Count);

                _cache.Set(AllVehiclesCacheKey, vehicles, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = VehicleCacheDuration
                });

                return vehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load vehicles");
                throw;
            }
        }

        public async Task<Vehicle?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(AllVehiclesCacheKey, out IReadOnlyList<Vehicle>? cachedVehicles))
            {
                var cached = cachedVehicles.FirstOrDefault(v => v.Id == id);
                if (cached != null)
                {
                    return cached;
                }
            }

            const string query = @"SELECT id, branch_id, vin, plate_number, make, model, model_year, category, transmission, fuel,
                                         seats, doors, color, description, daily_rate, status, created_at, updated_at
                                  FROM vehicles
                                  WHERE id = @id
                                  LIMIT 1";

            var parameters = new List<MySqlParameter>
            {
                new("@id", id)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                var record = rows.FirstOrDefault();
                return record == null ? null : MapVehicle(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load vehicle {VehicleId}", id);
                throw;
            }
        }

        public async Task<IReadOnlyList<Vehicle>> GetAvailableAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT v.id, v.branch_id, v.vin, v.plate_number, v.make, v.model, v.model_year, v.category, v.transmission,
                                         v.fuel, v.seats, v.doors, v.color, v.description, v.daily_rate, v.status, v.created_at, v.updated_at
                                  FROM vehicles v
                                  WHERE v.status = 'ACTIVE'
                                    AND NOT EXISTS (
                                        SELECT 1 FROM reservations r
                                        WHERE r.vehicle_id = v.id
                                          AND r.status IN ('PENDING', 'CONFIRMED')
                                          AND r.start_date < @endDate
                                          AND r.end_date > @startDate
                                    )";

            var parameters = new List<MySqlParameter>
            {
                new("@startDate", startDate),
                new("@endDate", endDate)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapVehicle).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load available vehicles between {Start} and {End}", startDate, endDate);
                throw;
            }
        }

        public async Task<bool> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            const string command = @"INSERT INTO vehicles (branch_id, vin, plate_number, make, model, model_year, category, transmission,
                                                            fuel, seats, doors, color, description, daily_rate, status)
                                     VALUES (@branchId, @vin, @plateNumber, @make, @model, @modelYear, @category, @transmission,
                                             @fuel, @seats, @doors, @color, @description, @dailyRate, @status)";

            var parameters = BuildVehicleParameters(vehicle, includeId: false);

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                if (rowsAffected > 0)
                {
                    InvalidateCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create vehicle with plate {Plate}", vehicle.PlateNumber);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            const string command = @"UPDATE vehicles
                                     SET branch_id = @branchId,
                                         vin = @vin,
                                         plate_number = @plateNumber,
                                         make = @make,
                                         model = @model,
                                         model_year = @modelYear,
                                         category = @category,
                                         transmission = @transmission,
                                         fuel = @fuel,
                                         seats = @seats,
                                         doors = @doors,
                                         color = @color,
                                         description = @description,
                                         daily_rate = @dailyRate,
                                         status = @status
                                     WHERE id = @id";

            var parameters = BuildVehicleParameters(vehicle, includeId: true);

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                if (rowsAffected > 0)
                {
                    InvalidateCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update vehicle {VehicleId}", vehicle.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            const string command = "DELETE FROM vehicles WHERE id = @id";
            var parameters = new List<MySqlParameter>
            {
                new("@id", id)
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                if (rowsAffected > 0)
                {
                    InvalidateCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vehicle {VehicleId}", id);
                throw;
            }
        }

        private List<MySqlParameter> BuildVehicleParameters(Vehicle vehicle, bool includeId)
        {
            var parameters = new List<MySqlParameter>
            {
                new("@branchId", vehicle.BranchId.HasValue ? vehicle.BranchId.Value : DBNull.Value),
                new("@vin", string.IsNullOrWhiteSpace(vehicle.Vin) ? DBNull.Value : vehicle.Vin!),
                new("@plateNumber", vehicle.PlateNumber),
                new("@make", vehicle.Make),
                new("@model", vehicle.Model),
                new("@modelYear", vehicle.ModelYear),
                new("@category", EnumToDatabase(vehicle.Category)),
                new("@transmission", EnumToDatabase(vehicle.Transmission)),
                new("@fuel", EnumToDatabase(vehicle.Fuel)),
                new("@seats", vehicle.Seats),
                new("@doors", vehicle.Doors),
                new("@color", string.IsNullOrWhiteSpace(vehicle.Color) ? DBNull.Value : vehicle.Color!),
                new("@description", string.IsNullOrWhiteSpace(vehicle.Description) ? DBNull.Value : vehicle.Description!),
                new("@dailyRate", vehicle.DailyRate),
                new("@status", EnumToDatabase(vehicle.Status))
            };

            if (includeId)
            {
                parameters.Add(new MySqlParameter("@id", vehicle.Id));
            }

            return parameters;
        }

        private static Vehicle MapVehicle(IDictionary<string, object> record)
        {
            var vehicle = new Vehicle();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                vehicle.Id = Convert.ToInt64(idObj);
            }

            if (record.TryGetValue("branch_id", out var branchObj) && branchObj is not DBNull)
            {
                vehicle.BranchId = Convert.ToInt64(branchObj);
            }

            if (record.TryGetValue("vin", out var vinObj) && vinObj is not DBNull)
            {
                vehicle.Vin = Convert.ToString(vinObj);
            }

            if (record.TryGetValue("plate_number", out var plateObj) && plateObj is not DBNull)
            {
                vehicle.PlateNumber = Convert.ToString(plateObj) ?? string.Empty;
            }

            if (record.TryGetValue("make", out var makeObj) && makeObj is not DBNull)
            {
                vehicle.Make = Convert.ToString(makeObj) ?? string.Empty;
            }

            if (record.TryGetValue("model", out var modelObj) && modelObj is not DBNull)
            {
                vehicle.Model = Convert.ToString(modelObj) ?? string.Empty;
            }

            if (record.TryGetValue("model_year", out var yearObj) && yearObj is not DBNull)
            {
                vehicle.ModelYear = Convert.ToInt16(yearObj);
            }

            if (record.TryGetValue("category", out var categoryObj) && categoryObj is not DBNull)
            {
                vehicle.Category = ParseEnum<VehicleCategory>(Convert.ToString(categoryObj));
            }

            if (record.TryGetValue("transmission", out var transmissionObj) && transmissionObj is not DBNull)
            {
                vehicle.Transmission = ParseEnum<TransmissionType>(Convert.ToString(transmissionObj));
            }

            if (record.TryGetValue("fuel", out var fuelObj) && fuelObj is not DBNull)
            {
                vehicle.Fuel = ParseEnum<FuelType>(Convert.ToString(fuelObj));
            }

            if (record.TryGetValue("seats", out var seatsObj) && seatsObj is not DBNull)
            {
                vehicle.Seats = Convert.ToByte(seatsObj);
            }

            if (record.TryGetValue("doors", out var doorsObj) && doorsObj is not DBNull)
            {
                vehicle.Doors = Convert.ToByte(doorsObj);
            }

            if (record.TryGetValue("color", out var colorObj) && colorObj is not DBNull)
            {
                vehicle.Color = Convert.ToString(colorObj);
            }

            if (record.TryGetValue("description", out var descriptionObj) && descriptionObj is not DBNull)
            {
                vehicle.Description = Convert.ToString(descriptionObj);
            }

            if (record.TryGetValue("daily_rate", out var rateObj) && rateObj is not DBNull)
            {
                vehicle.DailyRate = Convert.ToDecimal(rateObj);
            }

            if (record.TryGetValue("status", out var statusObj) && statusObj is not DBNull)
            {
                vehicle.Status = ParseEnum<VehicleStatus>(Convert.ToString(statusObj));
            }

            if (record.TryGetValue("created_at", out var createdObj) && createdObj is not DBNull)
            {
                vehicle.CreatedAt = Convert.ToDateTime(createdObj);
            }

            if (record.TryGetValue("updated_at", out var updatedObj) && updatedObj is not DBNull)
            {
                vehicle.UpdatedAt = Convert.ToDateTime(updatedObj);
            }

            return vehicle;
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

        private void InvalidateCache()
        {
            _cache.Remove(AllVehiclesCacheKey);
        }
    }
}
