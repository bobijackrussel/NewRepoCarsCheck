using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IVehiclePhotoService
    {
        Task<IReadOnlyList<VehiclePhoto>> GetByVehicleIdAsync(long vehicleId, CancellationToken cancellationToken = default);

        Task<IDictionary<long, VehiclePhoto?>> GetPrimaryPhotosAsync(IEnumerable<long> vehicleIds, CancellationToken cancellationToken = default);
    }
}
