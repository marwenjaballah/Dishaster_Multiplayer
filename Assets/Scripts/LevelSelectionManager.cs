using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative level selection and progression manager.
/// Synchronizes active level profile, rent parameters, courier distance curves,
/// and environmental lighting across all connected multiplayer clients.
/// </summary>
public class LevelSelectionManager : NetworkBehaviour
{
    public static LevelSelectionManager Instance { get; private set; }

    public static event EventHandler<OnLevelChangedEventArgs> OnAnyLevelChanged;

    public static void ResetStaticData()
    {
        OnAnyLevelChanged = null;
    }

    public class OnLevelChangedEventArgs : EventArgs
    {
        public LevelProfileSO activeLevel;
        public int levelIndex;
    }

    [Header("Available Level Profiles")]
    [SerializeField] private List<LevelProfileSO> levelProfiles = new List<LevelProfileSO>();

    [Header("Environment References")]
    [SerializeField] private Light mainDirectionalLight;

    private readonly NetworkVariable<int> _activeLevelIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private int _fallbackActiveIndex = 0;

    private int CurrentActiveIndex => (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        ? _activeLevelIndex.Value
        : _fallbackActiveIndex;

    public LevelProfileSO ActiveLevel => (levelProfiles != null && levelProfiles.Count > 0 && CurrentActiveIndex < levelProfiles.Count)
        ? levelProfiles[CurrentActiveIndex]
        : null;

    public int ActiveLevelIndex => CurrentActiveIndex;
    public int TotalLevels => levelProfiles != null ? levelProfiles.Count : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindMainLightIfMissing();
    }

    private void Start()
    {
        ApplyLevelLightingAndTheme(ActiveLevel);
    }

    public override void OnNetworkSpawn()
    {
        _activeLevelIndex.OnValueChanged += ActiveLevelIndex_OnValueChanged;
        NotifyLevelChanged();
    }

    public override void OnNetworkDespawn()
    {
        _activeLevelIndex.OnValueChanged -= ActiveLevelIndex_OnValueChanged;
    }

    private void ActiveLevelIndex_OnValueChanged(int oldIndex, int newIndex)
    {
        NotifyLevelChanged();
    }

    private void NotifyLevelChanged()
    {
        LevelProfileSO active = ActiveLevel;
        ApplyLevelLightingAndTheme(active);

        OnAnyLevelChanged?.Invoke(this, new OnLevelChangedEventArgs
        {
            activeLevel = active,
            levelIndex = _activeLevelIndex.Value
        });

        Debug.Log($"[LevelSelectionManager] Active level set to: {active?.levelName} (Index {_activeLevelIndex.Value})");
    }

    private void FindMainLightIfMissing()
    {
        if (mainDirectionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    mainDirectionalLight = l;
                    break;
                }
            }
        }
    }

    private void ApplyLevelLightingAndTheme(LevelProfileSO level)
    {
        if (level == null) return;

        FindMainLightIfMissing();

        if (mainDirectionalLight != null)
        {
            switch (_activeLevelIndex.Value)
            {
                case 0:
                    // Level 1: Sunny Side Diner (Warm sunshine)
                    mainDirectionalLight.color = new Color(1.0f, 0.96f, 0.88f);
                    mainDirectionalLight.intensity = 2.0f;
                    RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.38f);
                    break;
                case 1:
                    // Level 2: Downtown Burger Bar (Urban evening dusk / electric neon)
                    mainDirectionalLight.color = new Color(0.70f, 0.82f, 1.0f);
                    mainDirectionalLight.intensity = 1.6f;
                    RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.40f);
                    break;
                case 2:
                    // Level 3: Boardwalk Grill (Golden coastal sunset)
                    mainDirectionalLight.color = new Color(1.0f, 0.72f, 0.45f);
                    mainDirectionalLight.intensity = 2.2f;
                    RenderSettings.ambientLight = new Color(0.40f, 0.30f, 0.25f);
                    break;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSelectLevelRpc(int index)
    {
        if (levelProfiles == null || index < 0 || index >= levelProfiles.Count)
        {
            return;
        }

        // Verify progression unlock if GameSaveManager exists
        if (Save.GameSaveManager.Instance != null && !Save.GameSaveManager.Instance.IsLevelUnlocked(index))
        {
            Debug.LogWarning($"[LevelSelectionManager] Cannot select Level {index}: Level is still locked in campaign!");
            return;
        }

        SelectLevel(index);
    }

    public void SelectNextLevel()
    {
        if (levelProfiles == null || levelProfiles.Count == 0) return;
        int nextIndex = (CurrentActiveIndex + 1) % levelProfiles.Count;
        SelectLevel(nextIndex);
    }

    public void SelectLevel(int index)
    {
        if (levelProfiles == null || index < 0 || index >= levelProfiles.Count) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!IsServer) return;
            _activeLevelIndex.Value = index;
        }
        else
        {
            _fallbackActiveIndex = index;
            NotifyLevelChanged();
        }
    }

    public LevelProfileSO GetLevelProfile(int index)
    {
        if (levelProfiles != null && index >= 0 && index < levelProfiles.Count)
        {
            return levelProfiles[index];
        }
        return null;
    }

    public void SetLevelProfilesList(List<LevelProfileSO> profiles)
    {
        levelProfiles = profiles;
    }
}
