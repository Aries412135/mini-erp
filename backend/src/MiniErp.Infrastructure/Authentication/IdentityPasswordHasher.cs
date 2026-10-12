using Microsoft.AspNetCore.Identity;
using MiniErp.Application.Common.Interface;

namespace MiniErp.Infrastructure.Authentication;

public class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(null!, password);

    public bool Verify(string PasswordHasher, string password)
        => _hasher.VerifyHashedPassword(null!, PasswordHasher, password)
            != PasswordVerificationResult.Failed;
}