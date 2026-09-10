using System;
using System.Threading.Tasks;
using HoldMyBeer.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldMyBeer.Networking
{
    /// <inheritdoc cref="INetworkSessionService"/>
    public sealed class NetworkSessionService : INetworkSessionService, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly ISessionTransportProvider _transports;
        private readonly IPlayerProfile _profile;

        private SessionStatus _status = SessionStatus.Offline;

        public NetworkSessionService(
            NetworkManager networkManager,
            ISessionTransportProvider transports,
            IPlayerProfile profile)
        {
            _networkManager = networkManager != null
                ? networkManager
                : throw new ArgumentNullException(nameof(networkManager));
            _transports = transports ?? throw new ArgumentNullException(nameof(transports));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));

            _networkManager.OnClientStopped += HandleClientStopped;
            _networkManager.OnServerStopped += HandleServerStopped;
        }

        public SessionStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                StatusChanged?.Invoke(value);
            }
        }

        public bool IsHost => _networkManager != null && _networkManager.IsHost;

        public string JoinCode { get; private set; } = string.Empty;

        public event Action<string> SessionEnded;

        public event Action<SessionStatus> StatusChanged;

        public async Task<SessionResult> HostAsync(SessionRequest request)
        {
            if (Status != SessionStatus.Offline)
            {
                return SessionResult.Fail("A session is already running.");
            }

            Status = SessionStatus.Starting;

            if (!_transports.TryGet(request.Mode, out var transport))
            {
                return FailAndReset($"Mode '{request.Mode}' has no transport registered.");
            }

            var configured = await transport.ConfigureHostAsync(request);
            if (!configured.Success)
            {
                return FailAndReset(configured.Error);
            }

            ApplyLocalConnectionData();

            if (!_networkManager.StartHost())
            {
                return FailAndReset("NetworkManager.StartHost() failed. Is the port already in use?");
            }

            JoinCode = configured.JoinCode;
            Status = SessionStatus.Connected;
            return SessionResult.Ok(JoinCode);
        }

        public async Task<SessionResult> JoinAsync(SessionRequest request)
        {
            if (Status != SessionStatus.Offline)
            {
                return SessionResult.Fail("A session is already running.");
            }

            Status = SessionStatus.Starting;

            if (!_transports.TryGet(request.Mode, out var transport))
            {
                return FailAndReset($"Mode '{request.Mode}' has no transport registered.");
            }

            var configured = await transport.ConfigureClientAsync(request);
            if (!configured.Success)
            {
                return FailAndReset(configured.Error);
            }

            ApplyLocalConnectionData();

            if (!_networkManager.StartClient())
            {
                return FailAndReset("NetworkManager.StartClient() failed.");
            }

            // StartClient only means "the attempt started". Approval, version checks
            // and timeouts land later, through OnClientStopped.
            Status = SessionStatus.Connected;
            return SessionResult.Ok();
        }

        public void Leave()
        {
            if (Status == SessionStatus.Offline || _networkManager == null)
            {
                return;
            }

            Status = SessionStatus.Disconnecting;
            _networkManager.Shutdown();
        }

        public void LoadNetworkScene(string sceneName)
        {
            if (_networkManager == null || !_networkManager.IsServer)
            {
                Debug.LogWarning("LoadNetworkScene is server-only and was ignored.");
                return;
            }

            var status = _networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Network scene load of '{sceneName}' refused: {status}.");
            }
        }

        public void Dispose()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnClientStopped -= HandleClientStopped;
            _networkManager.OnServerStopped -= HandleServerStopped;
        }

        private void ApplyLocalConnectionData()
        {
            _networkManager.NetworkConfig.ConnectionData =
                ConnectionPayload.Serialize(_profile.DisplayName, Application.version);
        }

        private SessionResult FailAndReset(string error)
        {
            Debug.LogError($"[Session] {error}");
            JoinCode = string.Empty;
            Status = SessionStatus.Offline;
            return SessionResult.Fail(error);
        }

        private void HandleClientStopped(bool wasHost)
        {
            // The host also stops as a client; let HandleServerStopped own that case.
            if (wasHost)
            {
                return;
            }

            EndSession(_networkManager.DisconnectReason);
        }

        private void HandleServerStopped(bool wasHost) => EndSession(string.Empty);

        private void EndSession(string reason)
        {
            JoinCode = string.Empty;
            Status = SessionStatus.Offline;
            SessionEnded?.Invoke(string.IsNullOrEmpty(reason) ? "Session closed." : reason);
        }
    }
}
