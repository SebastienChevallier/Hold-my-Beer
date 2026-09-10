using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Finds every <see cref="ISessionTransportInstaller"/> in the loaded assemblies.
    /// Runs once, at boot; never in a hot path.
    /// </summary>
    public static class TransportInstallerScanner
    {
        public static IEnumerable<ISessionTransport> DiscoverTransports(NetworkManager networkManager)
        {
            var results = new List<ISessionTransport>();
            var contract = typeof(ISessionTransportInstaller);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Only our own assemblies can contain installers: skip the ~200 others.
                if (!assembly.GetName().Name.StartsWith("HoldMyBeer", StringComparison.Ordinal))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface || !contract.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    try
                    {
                        var installer = (ISessionTransportInstaller)Activator.CreateInstance(type);
                        results.Add(installer.Create(networkManager));
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Transport installer '{type.Name}' failed: {exception.Message}");
                    }
                }
            }

            return results;
        }
    }
}
