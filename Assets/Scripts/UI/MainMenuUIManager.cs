using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance { get; private set; }
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playerOptionsMenu;
    [SerializeField] private GameObject soundOptionsMenu;
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
    [SerializeField] private Image cardThemeBackground;
    [SerializeField] private Image cardThemeBoard;
    [SerializeField] private Image cardThemePieces;
    [SerializeField] private TextMeshProUGUI cardThemeText;
    [SerializeField] private SpriteRenderer backgroundGame;
    [SerializeField] private SpriteRenderer boardGame;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetDefaultValues();
        SetDefaultListeners();
        LoadTheme(ProfileManager.Instance.GetPlayerTheme());
        LoadDefaultVolumes();
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
            backToOptionsButton.gameObject.SetActive(true);
        });

        soundOptionsButton.onClick.AddListener(() => {
            optionsMenu.SetActive(false);
            soundOptionsMenu.SetActive(true);
            backToOptionsButton.gameObject.SetActive(true);
        });

        backToOptionsButton.onClick.AddListener(() => {
            optionsMenu.SetActive(true);
            playerOptionsMenu.SetActive(false);
            soundOptionsMenu.SetActive(false);
            backToOptionsButton.gameObject.SetActive(false);
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

        masterSlider.onValueChanged.AddListener((float value) => {
            SetMasterVolume(value);
        });

        musicSlider.onValueChanged.AddListener((float value) => {
            SetMusicVolume(value);
        });

        sfxSlider.onValueChanged.AddListener((float value) => {
            SetSFXVolume(value);
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
        soundOptionsMenu.SetActive(false);
        backToOptionsButton.gameObject.SetActive(false);
    }

    private void LoadTheme(PlayerTheme playerTheme) {
        if (playerTheme != PlayerTheme.None) {
            Sprite backgroundSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Background");
            Sprite boardSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Board");

            backgroundGame.sprite = backgroundSprite;
            boardGame.sprite = boardSprite;

            Sprite cardBackgroundSprite = Resources.Load<Sprite>($"CardThemes/{playerTheme}/Background");
            Sprite cardBoardSprite = Resources.Load<Sprite>($"CardThemes/{playerTheme}/Board");
            Sprite cardPiecesSprite = Resources.Load<Sprite>($"CardThemes/{playerTheme}/Pieces");

            cardThemeBackground.sprite = cardBackgroundSprite;
            cardThemeBoard.sprite = cardBoardSprite;
            cardThemePieces.sprite = cardPiecesSprite;
            cardThemeText.text = playerTheme.ToString();
        }

        
    }

    private void LoadDefaultVolumes() {
        masterSlider.value = SoundManager.Instance.LoadPlayerMasterVolume();
        musicSlider.value = SoundManager.Instance.LoadPlayerMusicVolume();
        sfxSlider.value = SoundManager.Instance.LoadPlayerSFXVolume();
    }

    private void SetMasterVolume(float value) {
        audioMixer.SetFloat(AudioMixerParams.MasterVolume.ToString(), Mathf.Log10(value)*20);
        SoundManager.Instance.SetPlayerMasterVolume(value);
    }

    private void SetMusicVolume(float value) {
        audioMixer.SetFloat(AudioMixerParams.MusicVolume.ToString(), Mathf.Log10(value) * 20);
        SoundManager.Instance.SetPlayerMusicVolume(value);
    }

    private void SetSFXVolume(float value) {
        audioMixer.SetFloat(AudioMixerParams.SFXVolume.ToString(), Mathf.Log10(value) * 20);
        SoundManager.Instance.SetPlayerSFXVolume(value);
    }

    public void SetProfileImage(Sprite newSprite) {
        profileImage.sprite = newSprite;
    }

    public void SetPlayerTheme(PlayerTheme playerTheme) {
        selectThemeMenu.SetActive(false);
        LoadTheme(playerTheme);
    }
}
