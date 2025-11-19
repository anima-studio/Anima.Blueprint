using System;
using System.Linq;

namespace Moser.Solutions.Blueprint.Tests;

internal static class ResourceCollectionExtensions
{
    /// <summary>
    /// Keeps only the resources that match the given predicate.
    /// </summary>
    public static IResourceCollection Keep(this IResourceCollection resources, Func<IResource, bool> predicate)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        for (int i = resources.Count - 1; i >= 0; i--)
        {
            if (!predicate(resources[i]))
            {
                resources.RemoveAt(i);
            }
        }

        return resources;
    }

    /// <summary>
    /// Removes all resources that match the given predicate.
    /// </summary>
    public static IResourceCollection Remove(this IResourceCollection resources, Func<IResource, bool> predicate)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        for (int i = resources.Count - 1; i >= 0; i--)
        {
            if (predicate(resources[i]))
            {
                resources.RemoveAt(i);
            }
        }

        return resources;
    }

    /// <summary>
    /// Finds a resource by name.
    /// </summary>
    public static IResource? FindByName(this IResourceCollection resources, string name)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        return resources.FirstOrDefault(resource => string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a resource exists by name.
    /// </summary>
    public static bool Exists(this IResourceCollection resources, string name)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        return resources.Any(resource => string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a resource if it does not already exist in the collection.
    /// </summary>
    public static void AddIfNotExists(this IResourceCollection resources, IResource resource)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        if (resource == null) throw new ArgumentNullException(nameof(resource));

        if (!resources.Any(r => string.Equals(r.Name, resource.Name, StringComparison.OrdinalIgnoreCase)))
        {
            resources.Add(resource);
        }
    }
}
