using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;


//   call AudioManager.Instance.PlaySFX(clip) from anywhere

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance { get; private set; }

    [Header("music")]
    public float crossfadeDuration = 1.5f;
    public SceneMusic[] sceneMusicMap;

    [Header("defaults")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSFXVolume = 0.8f;

    [Header("pool size")]
    public int sfxPoolSize = 8;


    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKeyMaster, _masterVolume);
            ApplyVolumes();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKeyMusic, _musicVolume);
            ApplyVolumes();
        }
    }

    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKeySFX, _sfxVolume);
            ApplyVolumes();
        }
    }

    const string PrefKeyMaster = "AudioMasterVol";
    const string PrefKeyMusic = "AudioMusicVol";
    const string PrefKeySFX = "AudioSFXVol";

    float _masterVolume;
    float _musicVolume;
    float _sfxVolume;

    AudioSource _musicSourceA;
    AudioSource _musicSourceB;
    AudioSource _activeMusicSource;
    AudioSource[] _sfxPool;
    int _sfxPoolIndex;

    Coroutine _crossfadeRoutine;
    Dictionary<string, AudioClip> _sceneMusicLookup;
    AudioClip _currentMusicClip;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateAudioSources();
        LoadVolumes();
        BuildSceneMusicLookup();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }


    void CreateAudioSources()
    {

        _musicSourceA = CreateSource("Music A", true);
        _musicSourceB = CreateSource("Music B", true);
        _activeMusicSource = _musicSourceA;

        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
            _sfxPool[i] = CreateSource($"SFX {i}", false);
    }

    AudioSource CreateSource(string label, bool loop)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    public void LoadVolumes()
    {
        _masterVolume = PlayerPrefs.GetFloat(PrefKeyMaster, defaultMasterVolume);
        _musicVolume = PlayerPrefs.GetFloat(PrefKeyMusic, defaultMusicVolume);
        _sfxVolume = PlayerPrefs.GetFloat(PrefKeySFX, defaultSFXVolume);
        ApplyVolumes();
    }

    void BuildSceneMusicLookup()
    {
        _sceneMusicLookup = new Dictionary<string, AudioClip>();
        if (sceneMusicMap == null) return;

        foreach (SceneMusic entry in sceneMusicMap)
        {
            if (!string.IsNullOrEmpty(entry.sceneName) && entry.clip != null)
                _sceneMusicLookup[entry.sceneName] = entry.clip;
        }
    }


    void ApplyVolumes()
    {
        float musicFinal = _masterVolume * _musicVolume;
        _musicSourceA.volume = (_activeMusicSource == _musicSourceA) ? musicFinal : _musicSourceA.volume;
        _musicSourceB.volume = (_activeMusicSource == _musicSourceB) ? musicFinal : _musicSourceB.volume;

        float sfxFinal = _masterVolume * _sfxVolume;
        foreach (AudioSource src in _sfxPool)
        {
            if (src.isPlaying)
                src.volume = sfxFinal;
        }
    }

    float GetMusicVolume() => _masterVolume * _musicVolume;
    float GetSFXVolume() => _masterVolume * _sfxVolume;


    public void PlayMusic(AudioClip clip, float? fadeDuration = null)
    {
        if (clip == _currentMusicClip) return;

        float fade = fadeDuration ?? crossfadeDuration;
        _currentMusicClip = clip;

        if (_crossfadeRoutine != null)
            StopCoroutine(_crossfadeRoutine);

        _crossfadeRoutine = StartCoroutine(CrossfadeMusic(clip, fade));
    }


    public void StopMusic(float fadeDuration = 1f)
    {
        PlayMusic(null, fadeDuration);
    }

    public void PlayMusicImmediate(AudioClip clip)
    {
        if (clip == null) return;

        _currentMusicClip = clip;

        if (_crossfadeRoutine != null)
            StopCoroutine(_crossfadeRoutine);

        _musicSourceA.Stop();
        _musicSourceB.Stop();

        _activeMusicSource = _musicSourceA;
        _activeMusicSource.clip = clip;
        _activeMusicSource.volume = GetMusicVolume();
        _activeMusicSource.Play();
    }


    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource src = GetNextSFXSource();
        src.pitch = 1f;
        src.volume = GetSFXVolume() * volumeScale;
        src.PlayOneShot(clip);
    }


    public void PlaySFXRandomized(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        if (clip == null) return;

        AudioSource src = GetNextSFXSource();
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.volume = GetSFXVolume() * volumeScale;
        src.PlayOneShot(clip);
    }


    public void PlaySFXRandom(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFX(clips[Random.Range(0, clips.Length)], volumeScale);
    }

    AudioSource GetNextSFXSource()
    {
        AudioSource src = _sfxPool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;
        return src;
    }

    IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        AudioSource fadeOut = _activeMusicSource;
        AudioSource fadeIn = (fadeOut == _musicSourceA) ? _musicSourceB : _musicSourceA;
        _activeMusicSource = fadeIn;

        if (newClip != null)
        {
            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float targetVolume = GetMusicVolume();
        float startVolume = fadeOut.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float smoothT = t * t * (3f - 2f * t);

            fadeOut.volume = Mathf.Lerp(startVolume, 0f, smoothT);

            if (newClip != null)
                fadeIn.volume = Mathf.Lerp(0f, targetVolume, smoothT);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.clip = null;
        fadeOut.volume = 0f;

        if (newClip != null)
            fadeIn.volume = targetVolume;

        _crossfadeRoutine = null;
    }



    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_sceneMusicLookup == null) return;

        if (_sceneMusicLookup.TryGetValue(scene.name, out AudioClip clip))
        {
            PlayMusic(clip);
        }
 
    }



    [System.Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
    }
}