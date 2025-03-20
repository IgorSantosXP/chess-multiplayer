using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader
{
    public enum Scene {
        MainMenuScene,
        LobbyScene,
        GameScene,
        LoadingScene
    }

    private static Scene targetScene;

    public static void Load(Scene targetScene) {
        Debug.Log($"Caiu aqui");
        Loader.targetScene = targetScene;

        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoadNetwork(Scene targetScene) {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoaderCallback() {
        Debug.Log($"Agora aqui");
        SceneManager.LoadScene(targetScene.ToString());
    }
}
