using MiniErp.Domain.Entities;

namespace MiniErp.Application.Common.Interface;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken);
}