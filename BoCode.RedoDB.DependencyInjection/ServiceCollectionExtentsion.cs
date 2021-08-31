
using BoCode.RedoDB.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BoCode.RedoDB.DependencyInjection;

public static class ServiceCollectionExtentsion
{
    public static IServiceCollection AddRedoDB<I, T>(this IServiceCollection collection, ServiceLifetime serviceLifetime, Func<RedoDBEngineBuilder<T, I>, I> func)
        where T : class
        where I : class
    {
        if (collection is null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (func is null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        switch (serviceLifetime)
        {
            case ServiceLifetime.Scoped:
                collection.AddScoped<T>();
                collection.AddScoped<I>(serviceProvider => func(new RedoDBEngineBuilder<T, I>(serviceProvider.GetRequiredService<T>)));
                break;
            case ServiceLifetime.Singleton:
                collection.AddSingleton<T>();
                collection.AddSingleton<I>(serviceProvider => func(new RedoDBEngineBuilder<T, I>(serviceProvider.GetRequiredService<T>)));
                break;
            case ServiceLifetime.Transient:
                collection.AddTransient<T>();
                collection.AddTransient<I>(serviceProvider => func(new RedoDBEngineBuilder<T, I>(serviceProvider.GetRequiredService<T>)));
                break;
        }

        return collection;
    }
}
