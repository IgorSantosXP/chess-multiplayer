using Unity.Netcode;
using UnityEngine;

public class MainMenuCleanUp : MonoBehaviour
{
    private void Awake() {
        if (NetworkManager.Singleton != null) {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (ChessMultiplayer.Instance != null) {
            Destroy(ChessMultiplayer.Instance.gameObject);
        }

        if (ChessLobby.Instance != null) {
            Destroy(ChessLobby.Instance.gameObject);
        }
    }
}
