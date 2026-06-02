using UnityEngine;
using UnityEngine.UI;

namespace Gameville
{
    public class NetworkDiagnostics : MonoBehaviour
    {
        public MultiplayerManager multiplayerManager;
        public Text statusText;
        public bool showDebugPanel = true;

        private void Start()
        {
            if (multiplayerManager == null)
            {
                multiplayerManager = FindObjectOfType<MultiplayerManager>();
            }

            if (statusText != null && !showDebugPanel)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (statusText != null && showDebugPanel)
            {
                string mode = multiplayerManager != null && multiplayerManager.isHost ? "HOST" : "CLIENT";
                string status = GetConnectionStatus();
                statusText.text = $"Mode: {mode}\nStatus: {status}\nPress F1 to toggle debug panel";
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                showDebugPanel = !showDebugPanel;
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(showDebugPanel);
                }
            }
        }

        private string GetConnectionStatus()
        {
            if (multiplayerManager == null)
                return "MultiplayerManager not found";

            if (multiplayerManager.localPlayer == null)
                return "Local player not assigned";

            if (multiplayerManager.isHost)
            {
                return $"Host running on port {multiplayerManager.port}";
            }
            else
            {
                return $"Connecting to {multiplayerManager.connectAddress}:{multiplayerManager.port}";
            }
        }
    }
}
