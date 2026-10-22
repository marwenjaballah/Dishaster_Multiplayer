using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Dynamic Music Engine for Dishaster: City Express.
/// Smoothly transitions soundtrack pitch, tempo, and dynamics across day loop shifts
/// (Morning Prep, Lunch Rush, Afternoon, Dinner Rush, Night Market) and crisis events.
/// </summary>
public class MusicManager : MonoBehaviour {

    private const string PLAYER_PREFS_MUSIC_VOLUME = "MusicVolume";

    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private float baseVolume = 0.3f;
    private float currentVolumeMultiplier = 1.0f;
    private Coroutine transitionCoroutine;


    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        baseVolume = PlayerPrefs.GetFloat(PLAYER_PREFS_MUSIC_VOLUME, 0.3f);
        ApplyAudioSettings();
    }

    private void Start() {
        DayLoopManager.OnAnyDayPhaseChanged += DayLoopManager_OnAnyDayPhaseChanged;

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnFireSpawned += KitchenCrisisManager_OnFireSpawned;
        }
    }

    private void OnDestroy() {
        DayLoopManager.OnAnyDayPhaseChanged -= DayLoopManager_OnAnyDayPhaseChanged;
        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnFireSpawned -= KitchenCrisisManager_OnFireSpawned;
        }
        if (Instance == this) {
            Instance = null;
        }
    }

    private void DayLoopManager_OnAnyDayPhaseChanged(object sender, DayLoopManager.OnDayPhaseChangedEventArgs e) {
        float targetPitch = 1.0f;
        float targetVolMult = 1.0f;

        switch (e.newPhase) {
            case DayLoopManager.DayPhase.Shift1_MorningPrep:
                targetPitch = 0.95f;   // Relaxed acoustic tempo
                targetVolMult = 0.85f;
                break;
            case DayLoopManager.DayPhase.Shift2_LunchRush:
                targetPitch = 1.05f;   // Upbeat tempo
                targetVolMult = 1.05f;
                break;
            case DayLoopManager.DayPhase.Shift3_Afternoon:
                targetPitch = 1.00f;   // Steady groove
                targetVolMult = 0.95f;
                break;
            case DayLoopManager.DayPhase.Shift4_DinnerRush:
                targetPitch = 1.15f;   // Fast adrenaline rush
                targetVolMult = 1.15f;
                break;
            case DayLoopManager.DayPhase.ClosingTime:
                targetPitch = 0.90f;   // Calming down
                targetVolMult = 0.80f;
                break;
            case DayLoopManager.DayPhase.DaySummary:
            case DayLoopManager.DayPhase.NightMarket:
                targetPitch = 0.92f;   // Night Market lounge vibe
                targetVolMult = 0.85f;
                break;
        }

        TransitionTo(targetPitch, targetVolMult, 2.0f);
    }

    private void KitchenCrisisManager_OnFireSpawned(object sender, KitchenCrisisManager.OnFireSpawnedEventArgs e) {
        // Spike music urgency during crisis
        TransitionTo(1.18f, 1.15f, 0.8f);
    }

    public void TransitionTo(float targetPitch, float targetVolMultiplier, float duration = 1.5f) {
        if (transitionCoroutine != null) {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionRoutine(targetPitch, targetVolMultiplier, duration));
    }

    private IEnumerator TransitionRoutine(float targetPitch, float targetVolMult, float duration) {
        float startPitch = audioSource.pitch;
        float startVolMult = currentVolumeMultiplier;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            audioSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            currentVolumeMultiplier = Mathf.Lerp(startVolMult, targetVolMult, t);
            ApplyAudioSettings();

            yield return null;
        }

        audioSource.pitch = targetPitch;
        currentVolumeMultiplier = targetVolMult;
        ApplyAudioSettings();
        transitionCoroutine = null;
    }

    private void ApplyAudioSettings() {
        if (audioSource != null) {
            audioSource.volume = Mathf.Clamp01(baseVolume * currentVolumeMultiplier);
        }
    }

    public void ChangeVolume() {
        baseVolume += 0.1f;
        if (baseVolume > 1.0f) {
            baseVolume = 0f;
        }
        ApplyAudioSettings();

        PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, baseVolume);
        PlayerPrefs.Save();
    }

    public void SetVolume(float newVolume) {
        baseVolume = Mathf.Clamp01(newVolume);
        ApplyAudioSettings();

        PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, baseVolume);
        PlayerPrefs.Save();
    }

    public float GetVolume() {
        return baseVolume;
    }
}