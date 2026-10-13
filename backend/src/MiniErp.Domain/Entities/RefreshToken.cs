namespace MiniErp.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public User User { get; private set; }

    private RefreshToken() { } // dibutuhkan EF Core

    public static RefreshToken Create(
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTime nowUtc,
        TimeSpan lifeTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (lifeTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifeTime), "Lifetime harus positif.");
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(lifeTime)
        };
    }

    public bool IsUsed => UsedAt is not null;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;
    public bool IsActive(DateTime nowUtc) => !IsUsed && !IsRevoked && !IsExpired(nowUtc);
    public void MarkUsed(DateTime nowUtc) => UsedAt ??= nowUtc;
    public void Revoke(DateTime nowUtc) => RevokedAt ??= nowUtc;
}