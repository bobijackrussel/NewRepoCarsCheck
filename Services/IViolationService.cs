using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IViolationService
    {
        Task<bool> ReportAsync(ViolationReport report, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ViolationReport>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ViolationReport>> GetByReservationAsync(long reservationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ViolationReport>> GetByUserAsync(long userId, CancellationToken cancellationToken = default);
    }
}
