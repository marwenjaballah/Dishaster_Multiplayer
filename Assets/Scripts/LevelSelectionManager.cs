using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative level selection and progression manager.
/// Synchronizes active level profile, rent parameters, and courier distance curves
/// across all connected multiplayer clients.
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

    private readonly NetworkVariable<int> _activeLevelIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public LevelProfileSO ActiveLevel => (levelProfiles != null && levelProfiles.Count > 0 && _activeLevelIndex.Value < levelProfiles.Count)
        ? levelProfiles[_activeLevelIndex.Value]
        : null;

    public int ActiveLevelIndex => _activeLevelIndex.Value;
    public int TotalLevels => levelProfiles != null ? levelProfiles.Count : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        OnAnyLevelChanged?.Invoke(this, new OnLevelChangedEventArgs
        {
            activeLevel = ActiveLevel,
            levelIndex = _activeLevelIndex.Value
        });
    }

    public void SelectNextLevel()
    {
        if (!IsServer || levelProfiles == null || levelProfiles.Count == 0) return;
        _activeLevelIndex.Value = (_activeLevelIndex.Value + 1) % levelProfiles.Count;
    }

    public void SelectLevel(int index)
    {
        if (!IsServer || levelProfiles == null || index < 0 || index >= levelProfiles.Count) return;
        _activeLevelIndex.Value = index;
    }

    public LevelProfileSO GetLevelProfile(int index)
    {
        if (levelProfiles != null && index >= 0 && index < levelProfiles.Count)
        {
            return levelProfiles[index];
        }
        return null;
    }
}
