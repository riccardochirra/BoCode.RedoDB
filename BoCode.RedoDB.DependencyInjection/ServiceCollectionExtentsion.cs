
using BoCode.RedoDB.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BoCode.RedoDB.DependencyInjection;

public static class ServiceCollectionExtentsion
{
    public static IServiceCollection AddRedoDB<I, T>(this IServiceCollection collection, Func<RedoDBEngineBuilder<T,I>, I> func) 
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

        collection.AddSingleton<T>();
        
        collection.AddSingleton<I>(serviceProvider => func(new RedoDBEngineBuilder<T, I>(serviceProvider.GetRequiredService<T>)));

        return collection;
    }
}
