using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IReservationService
    {
        Task<IReadOnlyList<Reservation>> GetUserReservationsAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default);
        Task<bool> CancelAsync(long reservationId, string? reason = null, CancellationToken cancellationToken = default);
        Task<bool> IsVehicleAvailableAsync(long vehicleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
