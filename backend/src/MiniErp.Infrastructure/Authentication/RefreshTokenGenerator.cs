using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MiniErp.Application.Common.Interface;

namespace MiniErp.Infrastructure.Authentication;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtSettings _settings;

    public RefreshTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TimeSpan Lifetime => TimeSpan.FromDays(_settings.RefreshTokenDays);

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64Url.EncodeToString(bytes);
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}