using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;


    private void Awake() {
        closeButton.onClick.AddListener(Hide);
    }

    private void Start() {
        ChessMultiplayer.Instance.OnFailedToJoinGame += ChessMultiplayer_OnFailedToJoinGame;
        ChessMultiplayer.Instance.OnCreateLobbyCompleted += ChessLobby_OnCreateLobbyCompleted;
        ChessMultiplayer.Instance.OnJoinCompleted += ChessLobby_OnJoinCompleted;
        ChessLobby.Instance.OnCreateLobbyStarted += ChessLobby_OnCreateLobbyStarted;
        ChessLobby.Instance.OnCreateLobbyFailed += ChessLobby_OnCreateLobbyFailed;
        ChessLobby.Instance.OnJoinStarted += ChessLobby_OnJoinStarted;
        ChessLobby.Instance.OnJoinFailed += ChessLobby_OnJoinFailed;
        ChessLobby.Instance.OnQuickJoinFailed += ChessLobby_OnQuickJoinFailed;

        closeButton.gameObject.SetActive(false);
        Hide();
    }

    private void ChessLobby_OnJoinCompleted(object sender, System.EventArgs e) {
        Hide();
    }

    private void ChessLobby_OnCreateLobbyCompleted(object sender, System.EventArgs e) {
        Hide();
    }

    private void ChessLobby_OnQuickJoinFailed(object sender, System.EventArgs e) {
        ShowMessage("Could not find a Lobby to Quick Join!");
        closeButton.gameObject.SetActive(true);
    }

    private void ChessLobby_OnJoinFailed(object sender, System.EventArgs e) {
        ShowMessage("Failed to join Lobby!");
        closeButton.gameObject.SetActive(true);
    }

    private void ChessLobby_OnJoinStarted(object sender, System.EventArgs e) {
        ShowMessage("Joining Lobby...");
        closeButton.gameObject.SetActive(false);
    }

    private void ChessLobby_OnCreateLobbyFailed(object sender, System.EventArgs e) {
        ShowMessage("Failed to create Lobby!");
        closeButton.gameObject.SetActive(true);
    }

    private void ChessLobby_OnCreateLobbyStarted(object sender, System.EventArgs e) {
        ShowMessage("Creating Lobby...");
        closeButton.gameObject.SetActive(false);
    }

    private void ChessMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e) {
        if (!LobbyUIManager.Instance.IsLobbyWindowActive()) return;
        if (NetworkManager.Singleton.DisconnectReason == "") {
            ShowMessage("Failed to connect");
            closeButton.gameObject.SetActive(true);
        } else {
            ShowMessage(NetworkManager.Singleton.DisconnectReason);
            closeButton.gameObject.SetActive(true);
        }
        LobbyUIManager.Instance.RedirectToServerListMenu();
    }

    private void ShowMessage(string message) {
        Show();
        messageText.text = message;
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        ChessMultiplayer.Instance.OnFailedToJoinGame -= ChessMultiplayer_OnFailedToJoinGame;
        ChessLobby.Instance.OnCreateLobbyStarted -= ChessLobby_OnCreateLobbyStarted;
        ChessLobby.Instance.OnCreateLobbyFailed -= ChessLobby_OnCreateLobbyFailed;
        ChessLobby.Instance.OnJoinStarted -= ChessLobby_OnJoinStarted;
        ChessLobby.Instance.OnJoinFailed -= ChessLobby_OnJoinFailed;
        ChessLobby.Instance.OnQuickJoinFailed -= ChessLobby_OnQuickJoinFailed;
    }
}
