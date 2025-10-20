using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IVehicleService
    {
        Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Vehicle?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Vehicle>> GetAvailableAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<bool> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
