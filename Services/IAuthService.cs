using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<bool> SignUpAsync(User user, string password, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    }
}
