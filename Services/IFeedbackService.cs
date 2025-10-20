using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IFeedbackService
    {
        Task<bool> SubmitAsync(Feedback feedback, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Feedback>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Feedback>> GetByUserAsync(long userId, CancellationToken cancellationToken = default);
    }
}
