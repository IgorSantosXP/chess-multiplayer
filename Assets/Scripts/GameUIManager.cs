using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class GameUIManager : NetworkBehaviour
{
    public static GameUIManager Instance;

    [SerializeField] private GameObject endGameWindow;
    [SerializeField] private GameObject timersUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject rematchRequestWindow;
    [SerializeField] private GameObject surrenderWindow;
    [SerializeField] private TextMeshProUGUI endGameTitle;
    [SerializeField] private TextMeshProUGUI endGameText;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private TextMeshProUGUI opponentLeftText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button acceptRematchButton;
    [SerializeField] private Button rejectRematchButton;
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button acceptSurrenderButton;
    [SerializeField] private Button declineSurrenderButton;

    private GameManager gameManager;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetButtons();
        ResetEndGameWindow();
        SetDefaultValues();
        gameManager = GameManager.Instance;
        BoardManager.Instance.OnEndGame += BoardManager_OnEndGame;
        gameManager.OnGameStarted += GameManager_OnGameStarted;
        gameManager.OnEndGame += GameManager_OnEndGame;
    }

    private void GameManager_OnEndGame(string title, string text) {
        OpenEndGameWindow(title, text);
    }

    private void GameManager_OnGameStarted(object sender, System.EventArgs e) {
        ResetEndGameWindow();
        endGameWindow.gameObject.SetActive(false);
        timersUI.SetActive(true);
        loadingUI.SetActive(false);
        surrenderButton.gameObject.SetActive(true);
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

        surrenderButton.onClick.AddListener(() => {
            OpenSurrenderWindow();
        });

        acceptSurrenderButton.onClick.AddListener(() => {
            AcceptSurrender();
        });

        declineSurrenderButton.onClick.AddListener(() => {
            CloseSurrenderWindow();
        });
    }

    private void SetDefaultValues() {
        endGameWindow.gameObject.SetActive(false);
        timersUI.SetActive(false);
        loadingUI.SetActive(true);
        surrenderButton.gameObject.SetActive(false);
    }

    private void ResetEndGameWindow() {
        endGameWindow.SetActive(false);
        rematchButton.gameObject.SetActive(true);
        waitingText.gameObject.SetActive(false);
        opponentLeftText.gameObject.SetActive(false);
        rematchRequestWindow.SetActive(false);
        surrenderWindow.SetActive(false);
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
    }

    private void CloseSurrenderWindow() {
        surrenderWindow.SetActive(false);
    }

    private void AcceptSurrender() {
        gameManager.TriggerOnSurrender();
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
