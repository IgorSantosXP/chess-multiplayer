using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public event EventHandler OnGameStarted;

    [SerializeField] private RectTransform whiteTimer;
    [SerializeField] private RectTransform blackTimer;
    [SerializeField] private TextMeshProUGUI whiteTimerText;
    [SerializeField] private TextMeshProUGUI blackTimerText;
    [SerializeField] private RectTransform whiteClockTransform;
    [SerializeField] private RectTransform blackClockTransform;
    [SerializeField] private Image whiteTimerBackground;
    [SerializeField] private Image blackTimerBackground;
    [SerializeField] private TMP_FontAsset whiteTimerFont;
    [SerializeField] private TMP_FontAsset blackTimerFont;
    [SerializeField] private Sprite whiteTimerClockSprite;
    [SerializeField] private Sprite blackTimerClockSprite;

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private static NetworkVariable<PlayerType> assignedType = new NetworkVariable<PlayerType>(PlayerType.None);
    private NetworkVariable<float> whiteTimeRemaining = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> blackTimeRemaining = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isGameRunning;
    private bool wantsRematch;
    private bool isOptionWindowOpen;
    //private float whiteTimeRemaining;
    //private float blackTimeRemaining;
    private Color whiteTimerBackgroundColor = new Color(1f, 1f, 1f);
    private Color blackTimerBackgroundColor = new Color(0f, 0f, 0f);
    private Color lastSecondsTimerBackgroundColor = new Color(0.7075472f, 0f, 0f);
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
        SoundManager.Instance.StopMainMenuMusic();
        SetTimerDefaultPosition();
        playerTheme = ProfileManager.Instance.GetPlayerTheme();
        boardManager = BoardManager.Instance;
        boardManager.OnPieceMove += BoardManager_OnPieceMove;
        boardManager.OnEndGame += BoardManager_OnEndGame;

        whiteTimeRemaining.OnValueChanged += (oldValue, newValue) => HandleTimeChanged();
        blackTimeRemaining.OnValueChanged += (oldValue, newValue) => HandleTimeChanged();

        UpdateTimerUI();
    }

    private void Update() {
        if (!isGameRunning) return;
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (isOptionWindowOpen) {
                isOptionWindowOpen = false;
                GameUIManager.Instance.SetOptionsWindow(isOptionWindowOpen);
                return;
            }
            isOptionWindowOpen = true;
            GameUIManager.Instance.SetOptionsWindow(isOptionWindowOpen);
        }
    }

    private void SetTimerDefaultPosition() {
        currentTimerDefaultPosition = whiteTimer.localPosition;
        opponentTimerDefaultPosition = blackTimer.localPosition;
    }

    private void SetTimerDefaultValues() {
        whiteTimerBackground.color = whiteTimerBackgroundColor;
        blackTimerBackground.color = blackTimerBackgroundColor;

        whiteTimerText.font = whiteTimerFont;
        blackTimerText.font = blackTimerFont;

        whiteClockTransform.GetComponent<Image>().sprite = whiteTimerClockSprite;
        blackClockTransform.GetComponent<Image>().sprite = blackTimerClockSprite;
    }

    private void BoardManager_OnEndGame(string title, string text) {
        isGameRunning = false;
    }

    private void HandleTimeChanged() {
        UpdateTimerUI();
        SetTimerUIValues(currentPlayablePlayerType.Value);
        SetTimerClockVisibility(currentPlayablePlayerType.Value);
    }

    private void UpdateTimerUI() {
        whiteTimerText.text = FormatTime(whiteTimeRemaining.Value);
        blackTimerText.text = FormatTime(blackTimeRemaining.Value);
    }

    private string FormatTime(float time) {
        int minutes = Mathf.FloorToInt(time / 60);
        float seconds = time % 60;

        if (time < 20) {
            return string.Format("{0:0}:{1:00.0}", minutes, seconds);
        } else {
            return string.Format("{0:0}:{1:00}", minutes, Mathf.FloorToInt(seconds));
        }
    }

    private void BoardManager_OnPieceMove(object sender, EventArgs e) {
        if (IsServer) {
            PlayerType playerType = currentPlayablePlayerType.Value == PlayerType.White ? PlayerType.Black : PlayerType.White;
            currentPlayablePlayerType.Value = playerType;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerRunTimerRpc() {
        StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer() {
        bool isWhiteLastSeconds = false;
        bool isBlackLastSeconds = false;
        float rotationAmount = -90;

        SetTimer();
        yield return new WaitForSeconds(1);
        while (isGameRunning) {
            if (currentPlayablePlayerType.Value == PlayerType.White) {
                if (whiteTimeRemaining.Value <= 0 && IsServer) {
                    OnTimeOutRpc(PlayerType.Black, PlayerType.White);
                    yield break;
                }
                if (whiteTimeRemaining.Value < 20 && !isWhiteLastSeconds) {
                    //SetTimerUIValues(PlayerType.White);
                    isWhiteLastSeconds = true;
                    TriggerSoundRpc(PlayerType.White);
                }

                float decrement = whiteTimeRemaining.Value <= 20 ? 0.1f : 1f;
                whiteTimeRemaining.Value = Mathf.Max(whiteTimeRemaining.Value - decrement, 0);

                UpdateTimerUI();
                RotateClockRpc(PlayerType.White, rotationAmount);
                yield return new WaitForSeconds(decrement);
            }
            if (currentPlayablePlayerType.Value == PlayerType.Black) {
                if (blackTimeRemaining.Value <= 0 && IsServer) {
                    OnTimeOutRpc(PlayerType.White, PlayerType.Black);
                    yield break;
                }
                if (blackTimeRemaining.Value < 20 && !isBlackLastSeconds) {
                    //SetTimerUIValues(PlayerType.Black);
                    isBlackLastSeconds = true;
                    TriggerSoundRpc(PlayerType.Black);
                }

                float decrement = blackTimeRemaining.Value <= 20 ? 0.1f : 1f;
                blackTimeRemaining.Value = Mathf.Max(blackTimeRemaining.Value - decrement, 0);

                UpdateTimerUI();
                RotateClockRpc(PlayerType.Black, rotationAmount);
                yield return new WaitForSeconds(decrement);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerSoundRpc(PlayerType playerType) {
        if (localPlayerType == playerType) {
            PlaySound(BoardSound.LastSeconds);
        }
    }

    private void SetTimerClockVisibility(PlayerType playerType) {
        if (playerType == PlayerType.White) {
            whiteClockTransform.gameObject.SetActive(true);
            blackClockTransform.gameObject.SetActive(false);
        }
        if (playerType == PlayerType.Black) {
            whiteClockTransform.gameObject.SetActive(false);
            blackClockTransform.gameObject.SetActive(true);
        }
    }

    private void SetTimerAlphaColor(PlayerType playerType) {
        Color whiteBackgroundColor = whiteTimerBackground.color;
        Color whiteTextColor = whiteTimerText.color;
        Color blackBackgroundColor = blackTimerBackground.color;
        Color blackTextColor = blackTimerText.color;

        if (playerType == PlayerType.White) {
            whiteBackgroundColor.a = 1f;
            whiteTextColor.a = 1f;
            blackBackgroundColor.a = 0.3f;
            blackTextColor.a = 0.5f;
        }
        if (playerType == PlayerType.Black) {
            whiteBackgroundColor.a = 0.3f;
            whiteTextColor.a = 0.5f;
            blackBackgroundColor.a = 1f;
            blackTextColor.a = 1f;
        }
        whiteTimerBackground.color = whiteBackgroundColor;
        whiteTimerText.color = whiteTextColor;
        blackTimerBackground.color = blackBackgroundColor;
        blackTimerText.color = blackTextColor;
    }

    private void SetTimerUIValues(PlayerType playerType) {
        Color whiteBackgroundColor = whiteTimerBackgroundColor;
        Color blackBackgroundColor = blackTimerBackgroundColor;
        TMP_FontAsset whiteFontAsset = whiteTimerFont;
        Sprite whiteSprite = whiteTimerClockSprite;

        if (playerType == PlayerType.White && whiteTimeRemaining.Value < 20f) {
            whiteBackgroundColor = lastSecondsTimerBackgroundColor;
            whiteFontAsset = blackTimerFont;
            whiteSprite = blackTimerClockSprite;
        }
        if (playerType == PlayerType.Black && blackTimeRemaining.Value < 20f) {
            blackBackgroundColor = lastSecondsTimerBackgroundColor;
        }

        whiteTimerBackground.color = whiteBackgroundColor;
        blackTimerBackground.color = blackBackgroundColor;
        whiteTimerText.font = whiteFontAsset;
        whiteClockTransform.GetComponent<Image>().sprite = whiteSprite;
        SetTimerAlphaColor(playerType);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RotateClockRpc(PlayerType playerType, float rotationAmount) {
        RectTransform targetClock = playerType == PlayerType.White ? whiteClockTransform : blackClockTransform;
        StartCoroutine(RotateTimerClockSmoothly(targetClock, rotationAmount, 0.1f));
    }

    private IEnumerator RotateTimerClockSmoothly(Transform target, float angle, float duration) {
        Quaternion startRotation = target.rotation;
        Quaternion endRotation = target.rotation * Quaternion.Euler(0, 0, angle);
        float elapsed = 0f;

        while (elapsed < duration) {
            target.rotation = Quaternion.Lerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.rotation = endRotation;
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
        SetTimerDefaultValues();
        StartCoroutine(RunTimer());
    }

    private void SetTimer() {
        if (!IsServer) return;
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
        SoundManager.Instance.PlaySFX(audioClip);
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
