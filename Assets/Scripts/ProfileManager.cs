using SFB;
using System;
using System.Buffers.Text;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;


public enum PlayerTheme {
    None,
    Classic,
    BubbleGum,
    Nature,
    Space
}

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";
    private const string PLAYER_PREFS_PLAYER_THEME = "PlayerTheme";
    private string playerName;
    private PlayerTheme playerTheme;
    private Sprite profileSprite;
    private string savedImagePath;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));
        string themeString = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_THEME, PlayerTheme.Classic.ToString());

        if (Enum.TryParse(themeString, out PlayerTheme parsedTheme)) {
            playerTheme = parsedTheme;
        } else {
            playerTheme = PlayerTheme.Classic;
        }
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

    public void SetPlayerTheme(PlayerTheme playerTheme) {
        this.playerTheme = playerTheme;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_THEME, playerTheme.ToString());
    }

    public PlayerTheme GetPlayerTheme() {
        return playerTheme;
    }

    public void OpenImagePicker() {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select a image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(ProcessImage(paths[0]));
        }
    }

    private IEnumerator ProcessImage(string path) {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        int maxSize = 256;
        int originalWidth = texture.width;
        int originalHeight = texture.height;

        float aspectRatio = (float)originalWidth / originalHeight;

        int targetWidth = maxSize;
        int targetHeight = maxSize;

        if (aspectRatio > 1f) {
            targetHeight = Mathf.RoundToInt(maxSize / aspectRatio);
        } else {
            targetWidth = Mathf.RoundToInt(maxSize * aspectRatio);
        }

        Texture2D resized = ResizeTextureGPU(texture, targetWidth, targetHeight);

        Sprite sprite = Sprite.Create(resized, new Rect(0, 0, resized.width, resized.height), Vector2.one * 0.5f);
        MainMenuUIManager.Instance.SetProfileImage(sprite);
        profileSprite = sprite;

        savedImagePath = Path.Combine(Application.persistentDataPath, "profile_image.png");
        File.WriteAllBytes(savedImagePath, imageData);

        yield return null;
    }

    Texture2D ResizeTextureGPU(Texture2D source, int newWidth, int newHeight) {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;

        Graphics.Blit(source, rt);

        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        RenderTexture.active = rt;
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();

        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.active = null;

        return resizedTexture;
    }

    private void LoadSavedImage() {
        savedImagePath = Path.Combine(Application.persistentDataPath, "profile_image.png");

        if (File.Exists(savedImagePath)) {
            byte[] imageData = File.ReadAllBytes(savedImagePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            int maxSize = 256;
            int originalWidth = texture.width;
            int originalHeight = texture.height;

            float aspectRatio = (float)originalWidth / originalHeight;

            int targetWidth = maxSize;
            int targetHeight = maxSize;

            if (aspectRatio > 1f) {
                targetHeight = Mathf.RoundToInt(maxSize / aspectRatio);
            } else {
                targetWidth = Mathf.RoundToInt(maxSize * aspectRatio);
            }

            Texture2D resized = ResizeTextureGPU(texture, targetWidth, targetHeight);

            Sprite sprite = Sprite.Create(resized, new Rect(0, 0, resized.width, resized.height), Vector2.one * 0.5f);
            MainMenuUIManager.Instance.SetProfileImage(sprite);
            profileSprite = sprite;
        }
    }
}
