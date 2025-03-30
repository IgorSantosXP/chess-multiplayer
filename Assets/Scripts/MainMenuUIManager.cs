using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance { get; private set; }
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playerOptionsMenu;
    [SerializeField] private GameObject selectThemeMenu;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button playerOptionsButton;
    [SerializeField] private Button soundOptionsButton;
    [SerializeField] private Button backToOptionsButton;
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private Button uploadImageButton;
    [SerializeField] private Button changeThemeButton;
    [SerializeField] private Button closeThemeMenuButton;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Image profileImage;
    [SerializeField] private SpriteRenderer backgroundGame;
    [SerializeField] private SpriteRenderer boardGame;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetDefaultValues();
        SetDefaultListeners();
        LoadTheme(ProfileManager.Instance.GetPlayerTheme());
    }

    private void SetDefaultListeners() {
        multiplayerButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.LobbyScene);
        });

        quitButton.onClick.AddListener(() => {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        });

        optionsButton.onClick.AddListener(() => {
            mainMenu.SetActive(false);
            optionsMenu.SetActive(true);
        });

        playerOptionsButton.onClick.AddListener(() => {
            optionsMenu.SetActive(false);
            playerOptionsMenu.SetActive(true);
        });

        soundOptionsButton.onClick.AddListener(() => {
            Debug.Log("soundOptionsButton");
        });

        backToOptionsButton.onClick.AddListener(() => {
            optionsMenu.SetActive(true);
            playerOptionsMenu.SetActive(false);
        });

        backToMainMenuButton.onClick.AddListener(() => {
            optionsMenu.SetActive(false);
            mainMenu.SetActive(true);
        });

        uploadImageButton.onClick.AddListener(() => {
            ProfileManager.Instance.OpenImagePicker();
        });

        changeThemeButton.onClick.AddListener(() => {
            selectThemeMenu.SetActive(true);
        });

        closeThemeMenuButton.onClick.AddListener(() => {
            selectThemeMenu.SetActive(false);
        });

        playerNameInput.text = ProfileManager.Instance.GetPlayerName();
        playerNameInput.onValueChanged.AddListener((string newText) => {
            ProfileManager.Instance.SetPlayerName(newText);
        });
    }

    private void SetDefaultValues() {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playerOptionsMenu.SetActive(false);
        selectThemeMenu.SetActive(false);
    }

    private void LoadTheme(PlayerTheme playerTheme) {
        if (playerTheme != PlayerTheme.None) {
            Sprite backgroundSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Background");
            Sprite boardSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Board");

            backgroundGame.sprite = backgroundSprite;
            boardGame.sprite = boardSprite;
        }
    }

    public void SetProfileImage(Sprite newSprite) {
        profileImage.sprite = newSprite;
    }

    public void SetPlayerTheme(PlayerTheme playerTheme) {
        selectThemeMenu.SetActive(false);
        LoadTheme(playerTheme);
    }
}
