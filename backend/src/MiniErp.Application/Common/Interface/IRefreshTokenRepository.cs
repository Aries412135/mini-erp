using MiniErp.Domain.Entities;

namespace MiniErp.Application.Common.Interface;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken token);

    /// <summary>
    /// Menandai token terpakai secara atomik. Mengembalikan false kalau token
    /// sudah terpakai, dicabut, atau kadaluwarsa (misalnya kalah dalam balapan (race condition))
    /// </summary>
    Task<bool> TryMarkUsedAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken);

    Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken);
}