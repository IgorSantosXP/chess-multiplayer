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

    public void SetCheckMark(bool isActive) {
        checkMarkTheme.SetActive(isActive);
    }
}
