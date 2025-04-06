using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChessMultiplayer : NetworkBehaviour
{
    public static ChessMultiplayer Instance { get; private set; }

    public const int MAX_PLAYER_AMOUNT = 2;
    private NetworkList<PlayerData> playerDataNetworkList;
    private bool isInGameScene;
    private NetworkVariable<int> gameTimer = new NetworkVariable<int>(180, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Dictionary<ulong, Sprite> playerImages = new Dictionary<ulong, Sprite>();

    public event EventHandler OnPlayerDataNetworkListChanged;
    public event EventHandler OnFailedToJoinGame;

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        isInGameScene = false;

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnSceneChanged(Scene oldScene, Scene newScene) {
        if (newScene.name == Loader.Scene.GameScene.ToString()) {
            isInGameScene = true;
            return;
        }
        isInGameScene = false;
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StartHost() {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
        
    }

    public void StartClient() {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId) {
        if (isInGameScene) return;
        playerImages.Clear();
        if (clientId == NetworkManager.ServerClientId) {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Server_OnClientDisconnectCallback;
            playerDataNetworkList.Clear();
            return;
        }
        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId) {
                playerDataNetworkList.RemoveAt(i);
            }
        }
        if (NetworkManager.Singleton.ConnectedClientsList.Count < MAX_PLAYER_AMOUNT) {
            LobbyUIManager.Instance.SetStartButtonActive(false);
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId) {
        if (isInGameScene) return;
        if (clientId == NetworkManager.ServerClientId) {
            playerDataNetworkList.Clear();
        }

        LobbyUIManager.Instance.SetStartButtonActive(false);

        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId
        });
        SetPlayerNameServerRpc(ProfileManager.Instance.GetPlayerName());
        
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        if (NetworkManager.Singleton.ConnectedClientsList.Count == MAX_PLAYER_AMOUNT) {
            LobbyUIManager.Instance.SetStartButtonActive(true);
            SendImageToServer(ProfileManager.Instance.GetProfileSprite());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId) {
        if (isInGameScene) return;
        LobbyUIManager.Instance.SetStartButtonActive(false);
        SetPlayerNameServerRpc(ProfileManager.Instance.GetPlayerName());
        SendImageToServer(ProfileManager.Instance.GetProfileSprite());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId) {
        if (isInGameScene) return;
        playerImages.Clear();
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Client_OnClientConnectedCallback;
        LobbyUIManager.Instance.OpenHostDisconnectWindow();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private void SendImageToServer(Sprite sprite) {
        if (sprite == null) return;

        Texture2D tex = sprite.texture;
        byte[] imageBytes = ImageConversion.EncodeToPNG(tex);

        SendProfileImageServerRpc(imageBytes);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendProfileImageServerRpc(byte[] imageBytes, ServerRpcParams rpcParams = default) {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        SendProfileImageRpc(senderClientId, imageBytes);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendProfileImageRpc(ulong clientId, byte[] imageBytes) {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        playerImages[clientId] = sprite;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId) {
        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            if (playerDataNetworkList[i].clientId == clientId) {
                return i;
            }
        }
        return -1;
    }

    public Dictionary<ulong, Sprite> GetPlayerImages() {
        return playerImages;
    }

    public NetworkList<PlayerData> GetPlayerDataNetworkList() {
        return playerDataNetworkList;
    }

    public void SetGameTimer(int timer) {
        if (IsServer) {
            gameTimer.Value = timer;
        }
    }

    public int GetGameTimer() {
        return gameTimer.Value;
    }
}
