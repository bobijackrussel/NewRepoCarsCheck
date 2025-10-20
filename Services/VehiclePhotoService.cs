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
    public class VehiclePhotoService : IVehiclePhotoService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<VehiclePhotoService> _logger;

        public VehiclePhotoService(IDatabaseService databaseService, ILogger<VehiclePhotoService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<VehiclePhoto>> GetByVehicleIdAsync(long vehicleId, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, vehicle_id, photo_url, caption, is_primary, created_at
                                   FROM vehicle_photos
                                   WHERE vehicle_id = @vehicleId
                                   ORDER BY is_primary DESC, created_at DESC";

            var parameters = new List<MySqlParameter>
            {
                new("@vehicleId", vehicleId)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapPhoto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load photos for vehicle {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<IDictionary<long, VehiclePhoto?>> GetPrimaryPhotosAsync(IEnumerable<long> vehicleIds, CancellationToken cancellationToken = default)
        {
            var ids = vehicleIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
            {
                return new Dictionary<long, VehiclePhoto?>();
            }

            var parameterNames = ids.Select((_, index) => $"@id{index}").ToList();
            var query = $@"SELECT vehicle_id, id, photo_url, caption, is_primary, created_at
                           FROM vehicle_photos
                           WHERE vehicle_id IN ({string.Join(",", parameterNames)})
                           ORDER BY vehicle_id, is_primary DESC, created_at DESC";

            var parameters = ids.Select((id, index) => new MySqlParameter(parameterNames[index], id)).ToList();

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                var results = new Dictionary<long, VehiclePhoto?>();

                foreach (var record in rows)
                {
                    if (!record.TryGetValue("vehicle_id", out var vehicleIdObj) || vehicleIdObj is DBNull)
                    {
                        continue;
                    }

                    var vehicleId = Convert.ToInt64(vehicleIdObj);
                    if (results.ContainsKey(vehicleId))
                    {
                        continue;
                    }

                    results[vehicleId] = MapPhoto(record);
                }

                foreach (var id in ids)
                {
                    results.TryAdd(id, null);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load primary photos for vehicles");
                throw;
            }
        }

        private static VehiclePhoto MapPhoto(IDictionary<string, object> record)
        {
            var photo = new VehiclePhoto();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                photo.Id = Convert.ToInt64(idObj);
            }

            if (record.TryGetValue("vehicle_id", out var vehicleIdObj) && vehicleIdObj is not DBNull)
            {
                photo.VehicleId = Convert.ToInt64(vehicleIdObj);
            }

            if (record.TryGetValue("photo_url", out var urlObj) && urlObj is not DBNull)
            {
                photo.PhotoUrl = Convert.ToString(urlObj) ?? string.Empty;
            }

            if (record.TryGetValue("caption", out var captionObj) && captionObj is not DBNull)
            {
                photo.Caption = Convert.ToString(captionObj);
            }

            if (record.TryGetValue("is_primary", out var primaryObj) && primaryObj is not DBNull)
            {
                photo.IsPrimary = Convert.ToBoolean(primaryObj);
            }

            if (record.TryGetValue("created_at", out var createdObj) && createdObj is not DBNull)
            {
                photo.CreatedAt = Convert.ToDateTime(createdObj);
            }

            return photo;
        }
    }
}
