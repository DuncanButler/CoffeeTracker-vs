using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeTracker.Integration.Tests
{
    /// <summary>
    /// Extension methods for IServiceCollection to help with testing
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Removes all registrations of the specified service type
        /// </summary>
        public static IServiceCollection RemoveAll<T>(this IServiceCollection services)
        {
            var serviceDescriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                services.Remove(serviceDescriptor);
            }
            return services;
        }
    }
}
