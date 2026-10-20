using Microsoft.EntityFrameworkCore;
using MiniErp.Application.Common.Interface;
using MiniErp.Domain.Entities;

namespace MiniErp.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;

    public RoleRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }
}