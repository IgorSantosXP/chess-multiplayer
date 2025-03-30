using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public event EventHandler OnGameStarted;

    [SerializeField] private RectTransform whiteTimer;
    [SerializeField] private RectTransform blackTimer;
    [SerializeField] private TextMeshProUGUI whiteTimerText;
    [SerializeField] private TextMeshProUGUI blackTimerText;

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private static NetworkVariable<PlayerType> assignedType = new NetworkVariable<PlayerType>(PlayerType.None);
    private NetworkVariable<float> whiteTimeRemaining = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> blackTimeRemaining = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isGameRunning;
    private bool wantsRematch;
    private BoardManager boardManager;
    private HashSet<ulong> playersWantingRematch = new HashSet<ulong>();

    private Vector3 currentTimerDefaultPosition;
    private Vector3 opponentTimerDefaultPosition;

    private PlayerTheme playerTheme;

    public event Action<string, string> OnEndGame;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetTimerDefaultPosition();
        playerTheme = ProfileManager.Instance.GetPlayerTheme();
        boardManager = BoardManager.Instance;
        boardManager.OnPieceMove += BoardManager_OnPieceMove;
        boardManager.OnEndGame += BoardManager_OnEndGame;

        whiteTimeRemaining.OnValueChanged += (oldValue, newValue) => UpdateTimerUI();
        blackTimeRemaining.OnValueChanged += (oldValue, newValue) => UpdateTimerUI();

        UpdateTimerUI();
    }

    private void SetTimerDefaultPosition() {
        currentTimerDefaultPosition = whiteTimer.localPosition;
        opponentTimerDefaultPosition = blackTimer.localPosition;
    }

    private void BoardManager_OnEndGame(string title, string text) {
        isGameRunning = false;
    }

    private void UpdateTimerUI() {
        whiteTimerText.text = FormatTime(whiteTimeRemaining.Value);
        blackTimerText.text = FormatTime(blackTimeRemaining.Value);
    }

    private string FormatTime(float time) {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void BoardManager_OnPieceMove(object sender, EventArgs e) {
        if (IsServer) {
            PlayerType playerType = currentPlayablePlayerType.Value == PlayerType.White ? PlayerType.Black : PlayerType.White;
            currentPlayablePlayerType.Value = playerType;
        }
    }

    private IEnumerator RunTimer() {
        bool isWhiteLastSeconds = false;
        bool isBlackLastSeconds = false;
        SetTimer();
        yield return new WaitForSeconds(1);
        while (isGameRunning) {
            if (currentPlayablePlayerType.Value == PlayerType.White) {
                if (whiteTimeRemaining.Value <= 0) {
                    OnTimeOutRpc(PlayerType.Black, PlayerType.White);
                    yield break;
                }
                if (whiteTimeRemaining.Value <= 20 && !isWhiteLastSeconds) {
                    isWhiteLastSeconds = true;
                    TriggerOnLastSecondsRpc(PlayerType.White);
                }
                whiteTimeRemaining.Value -= 1;
                UpdateTimerUI();
            }
            if (currentPlayablePlayerType.Value == PlayerType.Black) {
                if (blackTimeRemaining.Value <= 0) {
                    OnTimeOutRpc(PlayerType.White, PlayerType.Black);
                    yield break;
                }
                if (whiteTimeRemaining.Value <= 20 && !isBlackLastSeconds) {
                    isBlackLastSeconds = true;
                    TriggerOnLastSecondsRpc(PlayerType.Black);
                }
                blackTimeRemaining.Value -= 1;
                UpdateTimerUI();
            }

            yield return new WaitForSeconds(1);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnLastSecondsRpc(PlayerType playerType) {
        if (localPlayerType == playerType) {
            PlaySound(BoardSound.LastSeconds);
        }
    }

    public override void OnNetworkSpawn() {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback; ;
        if (!IsServer) return;
        StartCoroutine(ServerStartGame());
    }

    private IEnumerator ServerStartGame() {
        SetHostRandomPlayerType();
        yield return new WaitForSeconds(1);
        SetClientPlayerTypeClientRpc();
        CheckStartGame();
    }

    [ClientRpc]
    private void SetClientPlayerTypeClientRpc() {
        if (IsHost) return;
        SetClientPlayerType(assignedType.Value);
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        if (isGameRunning) {
            OnClientDisconnect();
        }

        GameUIManager.Instance.ShowOpponentLeftText();
    }

    private void OnClientDisconnect() {
        string title = "Opponent Disconnect";
        string text = "";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
        PlaySound(BoardSound.GameEnd);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnTimeOutRpc(PlayerType winner, PlayerType currentPlayer) {
        string title = localPlayerType == winner ? "Victory!" : "Defeat!";
        string text = $"{currentPlayer}'s time is over, {winner} has won!";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
        PlaySound(BoardSound.GameEnd);
    }

    private void SetHostRandomPlayerType() {
        assignedType.Value = GetRandomPlayerType();
        SetPlayerType(assignedType.Value);
    }

    private PlayerType GetRandomPlayerType() {
        return UnityEngine.Random.value > 0.5f ? PlayerType.White : PlayerType.Black;
    }

    private void SetClientPlayerType(PlayerType playerType) {
        PlayerType otherType = playerType == PlayerType.White ? PlayerType.Black : PlayerType.White;
        SetPlayerType(otherType);
    }

    private void CheckStartGame() {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2) {
            currentPlayablePlayerType.Value = PlayerType.White;
            StartGame();
        }
    }

    private void StartGame() {
        TriggerOnGameStartedRpc();
        SetTimerPositionRpc();
        AdjustCameraRotationRpc();
        StartCoroutine(RunTimer());
    }

    private void SetTimer() {
        whiteTimeRemaining.Value = ChessMultiplayer.Instance.GetGameTimer();
        blackTimeRemaining.Value = ChessMultiplayer.Instance.GetGameTimer();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetTimerPositionRpc() {
        if (localPlayerType == PlayerType.White) {

            whiteTimer.localPosition = currentTimerDefaultPosition;
            blackTimer.localPosition = opponentTimerDefaultPosition;
        }

        if (localPlayerType == PlayerType.Black) {
            whiteTimer.localPosition = opponentTimerDefaultPosition;
            blackTimer.localPosition = currentTimerDefaultPosition;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc() {
        isGameRunning = true;
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    private void SetPlayerType(PlayerType type) {
        localPlayerType = type;
    }

    public PlayerType GetLocalPlayerType() {
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType() {
        return currentPlayablePlayerType.Value;
    }

    private void RestartGame() {
        ResetDefaultValues();
        StartGame();
    }

    private void ResetDefaultValues() {
        currentPlayablePlayerType.Value = PlayerType.White;
        PlayerType newSide = assignedType.Value == PlayerType.White ? PlayerType.Black : PlayerType.White;
        assignedType.Value = newSide;
        SetPlayerType(newSide);
        playersWantingRematch.Clear();
        wantsRematch = false;
        ResetDefaultValuesClientRpc(newSide);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AdjustCameraRotationRpc() {
        if (localPlayerType == PlayerType.Black) {
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 180);
            return;
        }
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    [ClientRpc]
    private void ResetDefaultValuesClientRpc(PlayerType playerType) {
        if (IsHost) return;
        wantsRematch = false;
        SetClientPlayerType(playerType);
    }

    public void RequestRematch() {
        wantsRematch = true;

        SendRequestRematchRpc();
        RequestRematchServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRematchServerRpc(ulong clientId) {
        if (!playersWantingRematch.Contains(clientId)) {
            playersWantingRematch.Add(clientId);
        }

        if (playersWantingRematch.Count == NetworkManager.Singleton.ConnectedClientsIds.Count) {
            RestartGame();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendRequestRematchRpc() {
        if (wantsRematch) return;

        GameUIManager.Instance.SetRequestRematch();
    }

    private void PlaySound(BoardSound boardSound) {
        AudioClip audioClip = Resources.Load<AudioClip>($"Themes/{playerTheme}/Sounds/{boardSound}");
        SoundManager.Instance.PlaySound(audioClip);
    }

    public void TriggerOnSurrender() {
        PlayerType winner = localPlayerType == PlayerType.White ? PlayerType.Black : PlayerType.White;
        OnPlayerSurrenderRpc(winner, localPlayerType);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnPlayerSurrenderRpc(PlayerType winner, PlayerType currentPlayer) {
        string title = localPlayerType == winner ? "Victory!" : "Defeat!";
        string text = $"{currentPlayer} surrendered, {winner} won!";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
        PlaySound(BoardSound.GameEnd);
    }

    public Sprite FormatPlayerImageBase64(string base64Image) {
        byte[] imageData = Convert.FromBase64String(base64Image);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

        return sprite;
    }
}
