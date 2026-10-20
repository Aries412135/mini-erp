using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MiniErp.Api.Middleware;
using MiniErp.Api.Services;
using MiniErp.Application;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Common.Security;
using MiniErp.Infrastructure;
using MiniErp.Infrastructure.Authentication;
using MiniErp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Section 'Jwt' tidak ditemukan di konfigurasi.");

if (jwt.SecretKey.Length< 32)
{
    throw new InvalidOperationException("Jwt:SecretKey minimal 32 karakter.");
}

builder.Services
       .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.MapInboundClaims = false;

           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidIssuer = jwt.Issuer,

               ValidateAudience = true,
               ValidAudience = jwt.Audience,

               ValidateIssuerSigningKey = true,
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),

               ValidateLifetime = true,
               ClockSkew = TimeSpan.FromSeconds(30),

               NameClaimType = "name",
               RoleClaimType = AppClaims.Role
           };
       });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAdminAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();