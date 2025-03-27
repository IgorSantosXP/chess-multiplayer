using SFB;
using System.Collections;
using System.IO;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";
    private string playerName;
    private Sprite profileSprite;
    private string savedImagePath;

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + Random.Range(100, 1000));
    }

    private void Start() {
        LoadSavedImage();
    }

    public string GetPlayerName() {
        return playerName;
    }

    public void SetPlayerName(string playerName) {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    public Sprite GetProfileSprite() {
        return profileSprite;
    }

    public void OpenImagePicker() {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select a image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(LoadImage(paths[0]));
        }
    }

    private IEnumerator LoadImage(string path) {
        byte[] imageData = File.ReadAllBytes(path);

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        MainMenuUIManager.Instance.SetProfileImage(sprite);
        profileSprite = sprite;

        savedImagePath = Path.Combine(Application.persistentDataPath, "profile_image.png");
        File.WriteAllBytes(savedImagePath, imageData);

        yield return null;
    }

    private void LoadSavedImage() {
        Debug.Log($"Application.persistentDataPath: {Application.persistentDataPath}");
        savedImagePath = Path.Combine(Application.persistentDataPath, "profile_image.png");

        if (File.Exists(savedImagePath)) {
            byte[] imageData = File.ReadAllBytes(savedImagePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            profileSprite = sprite;

            MainMenuUIManager.Instance.SetProfileImage(sprite);
        }
    }
}
