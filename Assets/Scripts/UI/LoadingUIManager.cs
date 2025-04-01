using UnityEngine;

public class LoadingUIManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundGame;
    [SerializeField] private SpriteRenderer boardGame;

    private void Start() {
        LoadTheme(ProfileManager.Instance.GetPlayerTheme());
    }

    private void LoadTheme(PlayerTheme playerTheme) {
        if (playerTheme != PlayerTheme.None) {
            Sprite backgroundSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Background");
            Sprite boardSprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Board");

            backgroundGame.sprite = backgroundSprite;
            boardGame.sprite = boardSprite;
        }
    }
}
