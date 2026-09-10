using System;
using System.Collections.Generic;

namespace HoldMyBeer.Core
{
    /// <inheritdoc cref="IServiceContainer"/>
    public sealed class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _services[typeof(TService)] = instance;
        }

        public void Unregister<TService>() where TService : class
        {
            _services.Remove(typeof(TService));
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var found))
            {
                service = (TService)found;
                return true;
            }

            service = null;
            return false;
        }

        public TService Resolve<TService>() where TService : class
        {
            if (!TryResolve<TService>(out var service))
            {
                throw new ServiceNotRegisteredException(typeof(TService));
            }

            return service;
        }
    }
}
