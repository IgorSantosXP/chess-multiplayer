using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playerOptionsMenu;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button playerOptionsButton;
    [SerializeField] private Button soundOptionsButton;
    [SerializeField] private Button backToOptionsButton;
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private TMP_InputField playerNameInput;

    private void Start() {
        SetDefaultValues();
        SetDefaultListeners();
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

        playerNameInput.text = PlayerOptionsManager.Instance.GetPlayerName();
        playerNameInput.onValueChanged.AddListener((string newText) => {
            PlayerOptionsManager.Instance.SetPlayerName(newText);
        });
    }

    private void SetDefaultValues() {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playerOptionsMenu.SetActive(false);
    }
}
