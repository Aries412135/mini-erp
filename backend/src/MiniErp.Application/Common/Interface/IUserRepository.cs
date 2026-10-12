using MiniErp.Domain.Entities;

namespace MiniErp.Application.Common.Interface;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}