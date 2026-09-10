using HoldMyBeer.Core;
using HoldMyBeer.Networking;
using UnityEngine;

namespace HoldMyBeer.UI
{
    /// <summary>
    /// Owns the Menu scene. Menu and lobby live in the same scene: the lobby has no
    /// world content, and keeping them together removes a scene load in the middle
    /// of a connection handshake — which is exactly where it would hurt.
    ///
    /// Leaving the lobby for the game is NOT done here: the host triggers a network
    /// scene load, and every client (host included) follows automatically.
    /// </summary>
    public sealed class MenuSceneController : MonoBehaviour
    {
        private MainMenuScreen _menuScreen;
        private LobbyScreen _lobbyScreen;
        private IScreen _current;
        private INetworkSessionService _session;

        private void Start()
        {
            if (!AppServices.IsReady)
            {
                Debug.LogError(
                    "Menu scene entered without the Boot scene. " +
                    "Use Tools > Hold My Beer > Play From Boot.");
                enabled = false;
                return;
            }

            EnsureCamera();

            var container = AppServices.Container;
            _session = container.Resolve<INetworkSessionService>();

            var canvas = UiFactory.CreateCanvas("MenuCanvas");

            _menuScreen = new MainMenuScreen(
                _session,
                container.Resolve<ILobbyLauncher>(),
                container.Resolve<IPlayerProfile>(),
                () => SwitchTo(_lobbyScreen));

            _lobbyScreen = new LobbyScreen(
                container.Resolve<ILobbyProvider>(),
                _session,
                () => SwitchTo(_menuScreen));

            _menuScreen.Build(canvas.transform);
            _lobbyScreen.Build(canvas.transform);

            _menuScreen.Hide();
            _lobbyScreen.Hide();
            SwitchTo(_menuScreen);

            _session.SessionEnded += HandleSessionEnded;

            // Coming back from a game or a kick: make the cursor usable again.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.SessionEnded -= HandleSessionEnded;
            }

            _lobbyScreen?.Dispose();
        }

        private void SwitchTo(IScreen screen)
        {
            _current?.Hide();
            _current = screen;
            _current?.Show();
        }

        private void HandleSessionEnded(string reason)
        {
            SwitchTo(_menuScreen);
            _menuScreen.SetStatus(reason);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var go = new GameObject("MenuCamera", typeof(Camera));
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiFactory.Background;
        }
    }
}
