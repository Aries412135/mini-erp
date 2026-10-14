using Microsoft.EntityFrameworkCore;
using MiniErp.Application.Common.Interface;
using MiniErp.Domain.Entities;

namespace MiniErp.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;

    public RefreshTokenRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
    }

    public async Task<bool> TryMarkUsedAsync(Guid tokenId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await _db.RefreshTokens
            .Where(t => t.Id == tokenId
                        && t.UsedAt == null
                        && t.RevokedAt == null
                        && t.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.UsedAt, (DateTime?)nowUtc),
                cancellationToken);
        
        return rows == 1;
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        return _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, (DateTime?) nowUtc),
                cancellationToken);
    }
}