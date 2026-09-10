using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Finds the installers a feature declares, in any of our assemblies. Runs once,
    /// at boot; never in a hot path.
    ///
    /// This is what keeps optional modules truly optional: the composition root asks
    /// "what is installed?" instead of naming Relay or Steam, so deleting a folder
    /// removes a feature without a single edit elsewhere.
    /// </summary>
    public static class InstallerScanner
    {
        public static IEnumerable<ISessionTransport> DiscoverTransports(NetworkManager networkManager) =>
            Discover<ISessionTransportInstaller, ISessionTransport>(
                installer => installer.Create(networkManager));

        public static IEnumerable<ISessionInviteService> DiscoverInviteServices(NetworkManager networkManager) =>
            Discover<ISessionInviteInstaller, ISessionInviteService>(
                installer => installer.Create(networkManager));

        private static List<TResult> Discover<TInstaller, TResult>(Func<TInstaller, TResult> build)
            where TInstaller : class
            where TResult : class
        {
            var results = new List<TResult>();

            foreach (var type in OurTypes())
            {
                if (type.IsAbstract || type.IsInterface || !typeof(TInstaller).IsAssignableFrom(type))
                {
                    continue;
                }

                try
                {
                    var installer = (TInstaller)Activator.CreateInstance(type);

                    // A null result means "not usable in this run" (Steam not
                    // running, for instance), which is a normal outcome, not a bug.
                    var result = build(installer);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Installer '{type.Name}' failed: {exception.Message}");
                }
            }

            return results;
        }

        private static IEnumerable<Type> OurTypes()
        {
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
                    if (type != null)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
