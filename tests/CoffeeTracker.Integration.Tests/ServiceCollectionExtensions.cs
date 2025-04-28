using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to help with service collection management in tests.
    /// </summary>
    public static class TestServiceCollectionExtensions
    {
        /// <summary>
        /// Removes all registrations of a specific service type from an IServiceCollection.
        /// </summary>
        /// <typeparam name="T">The service type to remove.</typeparam>
        /// <param name="services">The service collection to modify.</param>
        public static IServiceCollection RemoveAll<T>(this IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
            return services;
        }
    }
}
