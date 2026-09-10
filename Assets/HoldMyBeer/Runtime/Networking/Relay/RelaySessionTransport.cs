using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HoldMyBeer.Networking;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace HoldMyBeer.Networking.Relay
{
    /// <summary>
    /// Unity Relay: the host allocates a slot on Unity's infrastructure and gets a
    /// short join code to share. Friends connect through the relay, so nobody has to
    /// open a port. Free tier, but it needs the project linked to a Unity Gaming
    /// Services project (Project Settings > Services).
    ///
    /// This lives in its own assembly on purpose: delete the folder and the rest of
    /// the game still compiles and runs on Direct IP.
    /// </summary>
    public sealed class RelaySessionTransport : ISessionTransport
    {
        private const string ConnectionType = "dtls";

        private readonly UnityTransport _transport;

        public RelaySessionTransport(UnityTransport transport)
        {
            _transport = transport != null ? transport : throw new ArgumentNullException(nameof(transport));
        }

        public SessionMode Mode => SessionMode.Relay;

        public async Task<SessionResult> ConfigureHostAsync(SessionRequest request)
        {
            var signedIn = await EnsureSignedInAsync();
            if (!signedIn.Success)
            {
                return signedIn;
            }

            try
            {
                // maxConnections counts the peers, host excluded.
                var maxConnections = Mathf.Max(1, request.MaxPlayers - 1);
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var endpoint = ResolveEndpoint(allocation.ServerEndpoints);
                if (endpoint == null)
                {
                    return SessionResult.Fail($"Relay returned no '{ConnectionType}' endpoint.");
                }

                _transport.SetRelayServerData(
                    endpoint.Host,
                    (ushort)endpoint.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    null,
                    endpoint.Secure);

                return SessionResult.Ok(joinCode);
            }
            catch (Exception exception)
            {
                return SessionResult.Fail($"Relay allocation failed: {exception.Message}");
            }
        }

        public async Task<SessionResult> ConfigureClientAsync(SessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.JoinCode))
            {
                return SessionResult.Fail("Enter the join code given by the host.");
            }

            var signedIn = await EnsureSignedInAsync();
            if (!signedIn.Success)
            {
                return signedIn;
            }

            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(request.JoinCode);

                var endpoint = ResolveEndpoint(allocation.ServerEndpoints);
                if (endpoint == null)
                {
                    return SessionResult.Fail($"Relay returned no '{ConnectionType}' endpoint.");
                }

                // A client must also pass the host's connection data, otherwise the
                // relay has no idea which allocation to forward its traffic to.
                _transport.SetRelayServerData(
                    endpoint.Host,
                    (ushort)endpoint.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    allocation.HostConnectionData,
                    endpoint.Secure);

                return SessionResult.Ok(request.JoinCode);
            }
            catch (Exception exception)
            {
                return SessionResult.Fail($"Could not join code '{request.JoinCode}': {exception.Message}");
            }
        }

        /// <summary>
        /// Relay advertises several endpoints (udp, dtls, wss). We want the encrypted
        /// one; falling back to the first offered keeps the game connectable if the
        /// service ever stops advertising dtls.
        /// </summary>
        private static RelayServerEndpoint ResolveEndpoint(List<RelayServerEndpoint> endpoints)
        {
            if (endpoints == null || endpoints.Count == 0)
            {
                return null;
            }

            foreach (var endpoint in endpoints)
            {
                if (string.Equals(endpoint.ConnectionType, ConnectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return endpoint;
                }
            }

            Debug.LogWarning($"Relay offered no '{ConnectionType}' endpoint; using '{endpoints[0].ConnectionType}'.");
            return endpoints[0];
        }

        private static async Task<SessionResult> EnsureSignedInAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return SessionResult.Ok();
            }
            catch (Exception exception)
            {
                return SessionResult.Fail(
                    "Unity Gaming Services sign-in failed. Link the project in " +
                    $"Project Settings > Services, then retry. ({exception.Message})");
            }
        }
    }
}
