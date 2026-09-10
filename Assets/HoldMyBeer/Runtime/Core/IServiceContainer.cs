using System;

namespace HoldMyBeer.Core
{
    /// <summary>
    /// Minimal composition root abstraction. Consumers depend on this, never on a
    /// concrete container, so the container can be swapped (VContainer, Zenject...)
    /// without touching gameplay code.
    /// </summary>
    public interface IServiceContainer
    {
        void Register<TService>(TService instance) where TService : class;
        void Unregister<TService>() where TService : class;
        bool TryResolve<TService>(out TService service) where TService : class;
        TService Resolve<TService>() where TService : class;
    }

    public sealed class ServiceNotRegisteredException : Exception
    {
        public ServiceNotRegisteredException(Type type)
            : base($"No service registered for contract '{type.FullName}'. " +
                   "Register it in the Boot scene before it is resolved.")
        {
        }
    }
}
