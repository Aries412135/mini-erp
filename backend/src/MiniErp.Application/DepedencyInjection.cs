using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Application.Common.Behaviors;

namespace MiniErp.Application;

public static class DepedencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        var assembly = Assembly.GetExecutingAssembly();

        service.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        service.AddValidatorsFromAssembly(assembly);

        return service;
    }
}