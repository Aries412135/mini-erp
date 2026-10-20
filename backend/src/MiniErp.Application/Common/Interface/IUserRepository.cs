using MiniErp.Domain.Entities;

namespace MiniErp.Application.Common.Interface;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
}