using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip mainMenuMusic;
    private const string PLAYER_PREFS_MASTER_VOLUME = "MasterVolume";
    private const string PLAYER_PREFS_MUSIC_VOLUME = "MusicVolume";
    private const string PLAYER_PREFS_SFX_VOLUME = "SFXVolume";

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFX(AudioClip clip) {
        sfxAudioSource.clip = clip;
        sfxAudioSource.Play();
    }

    public void StartMainMenuMusic() {
        musicAudioSource.clip = mainMenuMusic;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    public void StopMainMenuMusic() {
        musicAudioSource.loop = false;
        musicAudioSource.Stop();
    }

    public void SetPlayerMasterVolume(float value) {
        PlayerPrefs.SetFloat(PLAYER_PREFS_MASTER_VOLUME, value);
    }

    public void SetPlayerMusicVolume(float value) {
        PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, value);
    }

    public void SetPlayerSFXVolume(float value) {
        PlayerPrefs.SetFloat(PLAYER_PREFS_SFX_VOLUME, value);
    }

    public float LoadPlayerMasterVolume() {
        return PlayerPrefs.GetFloat(PLAYER_PREFS_MASTER_VOLUME, 0.6f);
    }

    public float LoadPlayerMusicVolume() {
        return PlayerPrefs.GetFloat(PLAYER_PREFS_MUSIC_VOLUME, 0.6f);
    }

    public float LoadPlayerSFXVolume() {
        return PlayerPrefs.GetFloat(PLAYER_PREFS_SFX_VOLUME, 0.6f);
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
    MoveSelf,
    MoveOpponent,
    MoveCheck,
    Promote
}

public enum AudioMixerParams {
    None,
    MasterVolume,
    MusicVolume,
    SFXVolume
}
