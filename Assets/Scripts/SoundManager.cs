using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [SerializeField] private AudioSource audioSource;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }
        Destroy(gameObject);
    }

    public void PlaySound(AudioClip clip) {
        audioSource.clip = clip;
        audioSource.Play();
    }
}

public enum BoardSound {
    None,
    Capture,
    Castle,
    GameEnd,
    GameStart,
    Illegal,
    LastSeconds,
    Move,
    MoveCheck,
    Promote
}
