using MiniErp.Domain.Entities;

namespace MiniErp.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0 , 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private static RefreshToken NewToken() =>
        RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Lifetime);

    [Fact]
    public void IsActive_NewToken_ReturnsTrue()
    {
        var token = NewToken();
        Assert.True(token.IsActive(Now.AddMinutes(1)));
    }

    [Fact]
    public void IsActive_AtExpiryTime_ReturnsFalse()
    {
        var token = NewToken();
        Assert.False(token.IsActive(Now.Add(Lifetime)));
    }

    [Fact]
    public void IsActive_AfterMarkUsed_ReturnsFalse()
    {
        var token = NewToken();
        token.MarkUsed(Now.AddMinutes(1));
        Assert.False(token.IsActive(Now.AddMinutes(2)));
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        var token = NewToken();
        token.Revoke(Now.AddMinutes(1));
        Assert.False(token.IsActive(Now.AddMinutes(2)));
    }

    [Fact]
    public void Revoke_CalledTwice_KeepsFirstTimestamp()
    {
        var token = NewToken();

        token.Revoke(Now.AddMinutes(1));
        token.Revoke(Now.AddMinutes(5));

        Assert.Equal(Now.AddMinutes(1), token.RevokedAt);
    }

    [Fact]
    public void Create_NonPositiveLifetime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, TimeSpan.Zero));
    }
}