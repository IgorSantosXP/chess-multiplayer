using UnityEngine.UI;
using UnityEngine;

public class ThemeUI : MonoBehaviour
{
    [SerializeField] private PlayerTheme playerTheme;
    [SerializeField] private Button selectThemeButton;
    [SerializeField] private GameObject checkMarkTheme;

    private void Awake() {
        selectThemeButton.onClick.AddListener(() => {
            ProfileManager.Instance.SetPlayerTheme(playerTheme);
            MainMenuUIManager.Instance.SetPlayerTheme(playerTheme);
        });
    }

    private void OnEnable() {
        if (ProfileManager.Instance.GetPlayerTheme() == playerTheme) {
            checkMarkTheme.SetActive(true);
            return;
        }
        checkMarkTheme.SetActive(false);
    }
}
