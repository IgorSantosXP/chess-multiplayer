using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : NetworkBehaviour
{
    public static GameUIManager Instance;

    [SerializeField] private GameObject endGameWindow;
    [SerializeField] private GameObject timersUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject rematchRequestWindow;
    [SerializeField] private GameObject surrenderWindow;
    [SerializeField] private GameObject currentCapturedPieces;
    [SerializeField] private GameObject opponentCapturedPieces;
    [SerializeField] private GameObject playersProfileContainer;
    [SerializeField] private GameObject optionsWindow;
    [SerializeField] private GameObject soundOptionsWindow;
    [SerializeField] private TextMeshProUGUI endGameTitle;
    [SerializeField] private TextMeshProUGUI endGameText;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private TextMeshProUGUI opponentLeftText;
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    [SerializeField] private TextMeshProUGUI opponentPlayerNameText;
    [SerializeField] private Image currentPlayerImage;
    [SerializeField] private Image opponentPlayerImage;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button acceptRematchButton;
    [SerializeField] private Button rejectRematchButton;
    [SerializeField] private Button acceptSurrenderButton;
    [SerializeField] private Button declineSurrenderButton;
    [SerializeField] private Button soundOptionButton;
    [SerializeField] private Button surrenderOptionButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private SpriteRenderer backgroundGame;
    [SerializeField] private SpriteRenderer boardGame;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;

    private GameManager gameManager;
    private BoardManager boardManager;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        LoadTheme(ProfileManager.Instance.GetPlayerTheme());
    }

    public override void OnNetworkSpawn() {
        SetButtons();
        ResetEndGameWindow();
        SetDefaultValues();
        SetPlayerName();
        boardManager = BoardManager.Instance;
        boardManager.OnEndGame += BoardManager_OnEndGame;
        boardManager.OnPieceCaptured += BoardManager_OnPieceCaptured;
        gameManager = GameManager.Instance;
        gameManager.OnGameStarted += GameManager_OnGameStarted;
        gameManager.OnEndGame += GameManager_OnEndGame;
        SetPlayerImage();
        LoadDefaultVolumes();
    }

    private void BoardManager_OnPieceCaptured(PlayerType playerType, PieceType pieceType) {
        if (playerType == gameManager.GetLocalPlayerType()) {
            foreach (Transform child in opponentCapturedPieces.transform) {
                CapturedPiece capturedPiece = child.GetComponent<CapturedPiece>();
                if (capturedPiece != null && capturedPiece.GetPieceType() == pieceType) {
                    capturedPiece.IncreasePieceCount();
                    int pieceCount = capturedPiece.GetPieceCount();
                    Sprite capturedPieceSprite = Resources.Load<Sprite>($"CapturedPieceSprites/{playerType}{pieceType}_{pieceCount}");
                    if (capturedPieceSprite != null) {
                        RectTransform rectTransform = child.GetComponent<RectTransform>();
                        float fixedHeight = rectTransform.sizeDelta.y;
                        float aspectRatio = capturedPieceSprite.rect.width / capturedPieceSprite.rect.height;
                        float newWidth = (fixedHeight * aspectRatio) + 2;
                        rectTransform.sizeDelta = new Vector2(newWidth, fixedHeight);

                        Image capturedPieceImage = child.GetComponent<Image>();
                        capturedPieceImage.sprite = capturedPieceSprite;
                        child.gameObject.SetActive(true);
                    }
                }
            }
            return;
        }
        foreach (Transform child in currentCapturedPieces.transform) {
            CapturedPiece capturedPiece = child.GetComponent<CapturedPiece>();
            if (capturedPiece != null && capturedPiece.GetPieceType() == pieceType) {
                capturedPiece.IncreasePieceCount();
                int pieceCount = capturedPiece.GetPieceCount();
                Sprite capturedPieceSprite = Resources.Load<Sprite>($"CapturedPieceSprites/{playerType}{pieceType}_{pieceCount}");
                if (capturedPieceSprite != null) {
                    RectTransform rectTransform = child.GetComponent<RectTransform>();
                    float fixedHeight = rectTransform.sizeDelta.y;
                    float aspectRatio = capturedPieceSprite.rect.width / capturedPieceSprite.rect.height;
                    float newWidth = (fixedHeight * aspectRatio) + 2;
                    rectTransform.sizeDelta = new Vector2(newWidth, fixedHeight);
                    Image capturedPieceImage = child.GetComponent<Image>();

                    capturedPieceImage.sprite = capturedPieceSprite;
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    private void GameManager_OnEndGame(string title, string text) {
        OpenEndGameWindow(title, text);
    }

    private void GameManager_OnGameStarted(object sender, System.EventArgs e) {
        SetCapturedPieces();
        ResetEndGameWindow();
        endGameWindow.SetActive(false);
        timersUI.SetActive(true);
        playersProfileContainer.SetActive(true);
        loadingUI.SetActive(false);
        optionsWindow.SetActive(false);
        soundOptionsWindow.SetActive(false);
        optionsButton.gameObject.SetActive(true);
    }

    private void BoardManager_OnEndGame(string title, string text) {
        OpenEndGameWindow(title, text);
    }

    private void SetButtons() {
        rematchButton.onClick.AddListener(() => {
            RequestRematch();
        });

        quitButton.onClick.AddListener(() => {
            QuitToMainMenu();
        });

        acceptRematchButton.onClick.AddListener(() => {
            AcceptRematch();
        });

        rejectRematchButton.onClick.AddListener(() => {
            RejectRematch();
        });

        acceptSurrenderButton.onClick.AddListener(() => {
            AcceptSurrender();
        });

        declineSurrenderButton.onClick.AddListener(() => {
            CloseSurrenderWindow();
        });

        soundOptionButton.onClick.AddListener(() => {
            OpenSoundOptions();
        });

        surrenderOptionButton.onClick.AddListener(() => {
            OpenSurrenderWindow();
        });

        backButton.onClick.AddListener(() => {
            BackToOptions();
        });

        optionsButton.onClick.AddListener(() => {
            SetOptionsWindow(optionsWindow.activeSelf ? false : true);
        });

        masterSlider.onValueChanged.AddListener((float value) => {
            SetMasterVolume(value);
        });

        sfxSlider.onValueChanged.AddListener((float value) => {
            SetSFXVolume(value);
        });

    }

    private void SetPlayerName() {
        foreach (PlayerData playerData in ChessMultiplayer.Instance.GetPlayerDataNetworkList()) {
            if (playerData.clientId == NetworkManager.Singleton.LocalClientId) {
                currentPlayerNameText.text = playerData.playerName.ToString();
            }

            if (playerData.clientId != NetworkManager.Singleton.LocalClientId) {
                opponentPlayerNameText.text = playerData.playerName.ToString();
            }
        }
    }

    private void SetPlayerImage() {
        foreach (KeyValuePair<ulong, Sprite> playerImage in ChessMultiplayer.Instance.GetPlayerImages()) {
            if (playerImage.Key == NetworkManager.Singleton.LocalClientId) {
                currentPlayerImage.sprite = playerImage.Value;
                currentPlayerImage.preserveAspect = true;
            }

            if (playerImage.Key != NetworkManager.Singleton.LocalClientId) {
                opponentPlayerImage.sprite = playerImage.Value;
                currentPlayerImage.preserveAspect = true;
            }
        }
    }

    private void SetCapturedPieces() {
        foreach (Transform child in currentCapturedPieces.transform) {
            CapturedPiece capturedPiece = child.GetComponent<CapturedPiece>();
            if (capturedPiece != null) {
                capturedPiece.ResetPieceCount();
            }
            child.gameObject.SetActive(false);
        }

        foreach (Transform child in opponentCapturedPieces.transform) {
            CapturedPiece capturedPiece = child.GetComponent<CapturedPiece>();
            if (capturedPiece != null) {
                capturedPiece.ResetPieceCount();
            }
            child.gameObject.SetActive(false);
        }
    }

    private void SetDefaultValues() {
        endGameWindow.SetActive(false);
        timersUI.SetActive(false);
        playersProfileContainer.SetActive(false);
        loadingUI.SetActive(true);
        optionsWindow.SetActive(false);
        soundOptionsWindow.SetActive(false);
        optionsButton.gameObject.SetActive(false);
    }

    private void ResetEndGameWindow() {
        endGameWindow.SetActive(false);
        rematchButton.gameObject.SetActive(true);
        waitingText.gameObject.SetActive(false);
        opponentLeftText.gameObject.SetActive(false);
        rematchRequestWindow.SetActive(false);
        surrenderWindow.SetActive(false);
        optionsWindow.SetActive(false);
        soundOptionsWindow.SetActive(false);
        optionsButton.gameObject.SetActive(false);
    }

    private void RequestRematch() {
        rematchButton.gameObject.SetActive(false);
        waitingText.gameObject.SetActive(true);

        gameManager.RequestRematch();
    }

    public void SetRequestRematch() {
        rematchButton.gameObject.SetActive(false);
        rematchRequestWindow.SetActive(true);
    }

    private void AcceptRematch() {
        gameManager.RequestRematch();
    }

    private void RejectRematch() {
        rematchButton.gameObject.SetActive(true);
        rematchRequestWindow.SetActive(false);
    }

    private void QuitToMainMenu() {
        NetworkManager.Singleton.Shutdown();
        Loader.Load(Loader.Scene.MainMenuScene);
    }

    private void OpenSurrenderWindow() {
        surrenderWindow.SetActive(true);
        optionsWindow.SetActive(false);
    }

    private void CloseSurrenderWindow() {
        surrenderWindow.SetActive(false);
    }

    private void AcceptSurrender() {
        gameManager.TriggerOnSurrender();
    }

    private void LoadTheme(PlayerTheme playerTheme) {
        if (playerTheme != PlayerTheme.None) {
            Sprite backgroundSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Background");
            Sprite boardSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Board");

            backgroundGame.sprite = backgroundSprite;
            boardGame.sprite = boardSprite;
        }
    }

    private void LoadDefaultVolumes() {
        masterSlider.value = SoundManager.Instance.LoadPlayerMasterVolume();
        sfxSlider.value = SoundManager.Instance.LoadPlayerSFXVolume();
    }

    private void SetMasterVolume(float value) {
        audioMixer.SetFloat(AudioMixerParams.MasterVolume.ToString(), Mathf.Log10(value) * 20);
        SoundManager.Instance.SetPlayerMasterVolume(value);
    }

    private void SetSFXVolume(float value) {
        audioMixer.SetFloat(AudioMixerParams.SFXVolume.ToString(), Mathf.Log10(value) * 20);
        SoundManager.Instance.SetPlayerSFXVolume(value);
    }

    private void OpenSoundOptions() {
        optionsWindow.SetActive(false);
        soundOptionsWindow.SetActive(true);
    }

    private void BackToOptions() {
        optionsWindow.SetActive(true);
        soundOptionsWindow.SetActive(false);
    }

    public void SetOptionsWindow(bool isOpen) {
        optionsWindow.SetActive(isOpen);
        soundOptionsWindow.SetActive(false);
        surrenderWindow.SetActive(false);
    }

    public void OpenEndGameWindow(string title, string text) {
        ResetEndGameWindow();
        endGameTitle.text = title;
        endGameText.text = text;
        endGameWindow.SetActive(true);
    }

    public void ShowOpponentLeftText() {
        if (endGameWindow == null) return;
        rematchButton.gameObject.SetActive(false);
        opponentLeftText.gameObject.SetActive(true);
        waitingText.gameObject.SetActive(false);
    }
}
