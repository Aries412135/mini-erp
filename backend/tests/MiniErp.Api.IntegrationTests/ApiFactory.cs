using System.Data;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace MiniErp.Api.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string SecretKey = "integration-tests-secret-key-0123456789-abcdef";
    public const string Issuer = "MiniErp.Api";
    public const string Audience = "MiniErp.Client";

    public ISender Sender { get; } = Substitute.For<ISender>();

    static ApiFactory()
    {
        // Diset lewat environment variable karena Program.cs membaca konfigurasi
        // sebelum host selesai dibangun
        Environment.SetEnvironmentVariable("Jwt__Secretkey", SecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=localhost;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // bukan Development: seeder admin tidak jalan
        
        builder.ConfigureTestServices(service =>
        {
            service.AddSingleton(Sender); // menimpa ISender asli milik MediatR
        });
    }
}