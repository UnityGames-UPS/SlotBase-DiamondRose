using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    internal static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _musicEnabled = PlayerPrefs.GetInt(PrefKeyMusic, 1) == 1;
        _sfxEnabled   = PlayerPrefs.GetInt(PrefKeysfx,   1) == 1;

        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private const string PrefKeyMusic = "audio_music_enabled";
    private const string PrefKeysfx   = "audio_sfx_enabled";

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Clips")]
    [SerializeField] private AudioClip clipSpinStopBtn;          // 1. spin/stop btn
    [SerializeField] private AudioClip clipSpinningLoop;         // 2. slot spinning sound
    [SerializeField] private AudioClip clipReelStop;             // 3. slot stop sound (once per slot)
    [SerializeField] private AudioClip clipWinLine;              // 4. win line sound
    [SerializeField] private AudioClip clipBigWinLoop;           // 5. big win mega win sound (play in loop)
    [SerializeField] private AudioClip clipButtonGeneric;        // 6. ui btn sound (all buttons except specific ones)
    [SerializeField] private AudioClip clipInfoPageBtn;          // 7. info page btn sound
    [SerializeField] private AudioClip clipInfoPageBackToGame;   // 8. info page back to game btn sound
    [SerializeField] private AudioClip clipPopupOpen;            // 9. reconnection/disconnection/loading/error popup open sound

    private bool _musicEnabled = true;
    private bool _sfxEnabled   = true;

    internal bool MusicEnabled => _musicEnabled;
    internal bool SfxEnabled   => _sfxEnabled;

    internal void SetMusicEnabled(bool on)
    {
        _musicEnabled = on;
        PlayerPrefs.SetInt(PrefKeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    internal void SetSfxEnabled(bool on)
    {
        _sfxEnabled = on;
        PlayerPrefs.SetInt(PrefKeysfx, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    private void ApplyMusicVolume()
    {
        // Dummy/no-op as there is no BG music source anymore.
    }

    private void ApplySfxVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = _sfxEnabled ? 1f : 0f;
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (!_sfxEnabled) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.clip  = clip;
        audioSource.loop  = true;
        audioSource.volume = _sfxEnabled ? 1f : 0f;
        audioSource.Play();
    }

    private void StopSource()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.clip  = null;
        audioSource.loop  = false;
    }

    // --- Active APIs for Game Sound Requirements ---

    internal void PlaySpinStopBtn()
    {
        PlayOneShot(clipSpinStopBtn);
    }

    internal void PlaySpinningLoop()
    {
        PlayLoop(clipSpinningLoop);
    }

    internal void PlayReelStop(bool isLastReel = false)
    {
        if (isLastReel)
        {
            StopSource();
        }
        PlayOneShot(clipReelStop);
    }

    internal void PlayWinLine()
    {
        PlayOneShot(clipWinLine); // Played once per win line animation loop
    }

    internal void StopWinLine()
    {
        // No-op because win line sound is a OneShot now
    }

    internal void PlayBigWinLoop()
    {
        PlayLoop(clipBigWinLoop);
    }

    internal void StopBigWinLoop()
    {
        if (audioSource != null && audioSource.loop && audioSource.clip == clipBigWinLoop)
        {
            StopSource();
        }
    }

    internal void PlayButtonGeneric()
    {
        PlayOneShot(clipButtonGeneric);
    }

    internal void PlayInfoPageBtn()
    {
        PlayOneShot(clipInfoPageBtn);
    }

    internal void PlayInfoPageBackToGameBtn()
    {
        PlayOneShot(clipInfoPageBackToGame);
    }

    internal void PlayPopupOpen()
    {
        PlayOneShot(clipPopupOpen);
    }



    private void OnApplicationFocus(bool hasFocus)
    {
        HandleFocus(hasFocus);
    }

    private void OnApplicationPause(bool isPaused)
    {
        HandleFocus(!isPaused);
    }

    private void HandleFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (audioSource != null) audioSource.Pause();
            AudioListener.volume = 0f;
        }
        else
        {
            if (audioSource != null) audioSource.UnPause();
            AudioListener.volume = 1f;
        }
    }
}

