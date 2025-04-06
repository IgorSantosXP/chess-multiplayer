using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [SerializeField] private GameObject serverListMenu;
    [SerializeField] private GameObject createLobbyMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button createLobbyBackButton;
    [SerializeField] private Button lobbyBackButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button kickPlayerButton;
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Toggle lobbyPrivateToggle;
    [SerializeField] private TMP_Dropdown timerDropdown;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;
    [SerializeField] private SpriteRenderer backgroundGame;
    [SerializeField] private SpriteRenderer boardGame;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI hostNameText;
    [SerializeField] private TextMeshProUGUI clientNameText;

    private int gameTimer;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        ResetDefaultValues();
        SetButtons();

        ChessMultiplayer.Instance.OnPlayerDataNetworkListChanged += ChessMultiplayer_OnPlayerDataNetworkListChanged;
        UpdatePlayerListUI();

        ChessLobby.Instance.OnLobbyListChanged += ChessLobby_OnLobbyListChanged;
        UpdateLobbyList(new List<Lobby>());
        LoadTheme(ProfileManager.Instance.GetPlayerTheme());
    }

    private void ChessLobby_OnLobbyListChanged(object sender, ChessLobby.OnLobbyListChangedEventArgs e) {
        UpdateLobbyList(e.lobbyList);
    }

    private void ChessMultiplayer_OnPlayerDataNetworkListChanged(object sender, EventArgs e) {
        UpdatePlayerListUI();
    }

    private void UpdatePlayerListUI() {
        if (!(ChessMultiplayer.Instance.GetPlayerDataNetworkList().Count > 0)) return;
        string hostName = ChessMultiplayer.Instance.GetPlayerDataNetworkList()[0].playerName.ToString();
        hostNameText.text = hostName;
        clientNameText.text = "Waiting...";

        if (ChessMultiplayer.Instance.GetPlayerDataNetworkList().Count == 2) {
            PlayerData clientData = ChessMultiplayer.Instance.GetPlayerDataNetworkList()[1];
            string clientName = clientData.playerName.ToString();
            clientNameText.text = clientName;
        }
    }

    private void ResetDefaultValues() {
        serverListMenu.SetActive(true);
        createLobbyMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        lobbyTemplate.gameObject.SetActive(false);
        gameTimer = 180;
    }

    private void SetButtons() {
        mainMenuButton.onClick.AddListener(() => {
            BackToMainMenu();
        });

        createLobbyButton.onClick.AddListener(() => {
            OpenCreateLobbyMenu();
        });

        quickJoinButton.onClick.AddListener(() => {
            ChessLobby.Instance.QuickJoin();
        });

        createLobbyBackButton.onClick.AddListener(() => {
            BackToServerListMenu();
        });

        lobbyBackButton.onClick.AddListener(() => {
            BackToServerListMenu();
        });

        joinCodeButton.onClick.AddListener(() => {
            ChessLobby.Instance.JoinWithCode(joinCodeInput.text);
        });

        startButton.onClick.AddListener(() => {
            StartGame();
        });

        timerDropdown.onValueChanged.AddListener(SetGameTimer);

        createButton.onClick.AddListener(() => {
            string lobbyName = lobbyNameInput.text;
            if (string.IsNullOrEmpty(lobbyName)) {
                lobbyName = "Lobby Name";
            }

            ChessLobby.Instance.CreateLobby(lobbyName, lobbyPrivateToggle.isOn, gameTimer);
        });
    }

    private void OpenCreateLobbyMenu() {
        serverListMenu.SetActive(false);
        createLobbyMenu.SetActive(true);
    }

    private void BackToMainMenu() {
        NetworkManager.Singleton.Shutdown();
        Loader.Load(Loader.Scene.MainMenuScene);
    }

    private void BackToServerListMenu() {
        serverListMenu.SetActive(true);
        createLobbyMenu.SetActive(false);
        lobbyMenu.SetActive(false);

        ChessLobby.Instance.LeaveLobby();
    }

    private void StartGame() {
        ChessLobby.Instance.DeleteLobby();
        Loader.LoadNetwork(Loader.Scene.GameScene);
    }

    private void SetGameTimer(int index) {
        int newTimer;

        switch (index) {
            case 0:
                newTimer = 180;
                break;
            case 1:
                newTimer = 300;
                break;
            case 2:
                newTimer = 600;
                break;
            default:
                newTimer = 180;
                break;
        }
        gameTimer = newTimer;
    }

    private void UpdateLobbyList(List<Lobby> lobbyList) {
        foreach (Transform child in lobbyContainer)
        {
            if (child == lobbyTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate, lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<LobbyListSingleUI>().SetLobby(lobby);
        }
    }

    private void LoadTheme(PlayerTheme playerTheme) {
        if (playerTheme != PlayerTheme.None) {
            Sprite backgroundSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Background");
            Sprite boardSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Board");

            backgroundGame.sprite = backgroundSprite;
            boardGame.sprite = boardSprite;
        }
    }

    public void OpenLobbyMenu(Lobby lobby) {
        createLobbyMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        serverListMenu.SetActive(false);
        kickPlayerButton.gameObject.SetActive(false);

        if (lobby != null) {
            lobbyNameText.text = lobby.Name;
            lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
        }
    }

    public void SetStartAndKickButtonActive(bool canActive) {
        startButton.gameObject.SetActive(canActive);
        kickPlayerButton.gameObject.SetActive(canActive);
    }

    public bool IsLobbyWindowActive() {
        return lobbyMenu.activeSelf;
    }

    public void RedirectToServerListMenu() {
        BackToServerListMenu();
    }

    public void SetKickPlayerButtonListener(string playerId, ulong clientId) {
        kickPlayerButton.onClick.AddListener(() => {
            ChessLobby.Instance.KickPlayer(playerId);
            ChessMultiplayer.Instance.KickPlayer(clientId);
        });
    }

    private void OnDestroy() {
        ChessLobby.Instance.OnLobbyListChanged -= ChessLobby_OnLobbyListChanged;
    }
}
