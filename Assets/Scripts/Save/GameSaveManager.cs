using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Save {

    /// <summary>
    /// Local campaign persistence manager.
    /// Serializes career bank savings, unlocked levels, star ratings per level,
    /// and purchased Night Market upgrades to a secure JSON file in Application.persistentDataPath.
    /// Server/Host authoritative in multiplayer lobbies.
    /// </summary>
    public class GameSaveManager : MonoBehaviour {

        public static GameSaveManager Instance { get; private set; }

        public static event EventHandler<OnSaveDataLoadedEventArgs> OnAnySaveDataLoaded;
        public static event EventHandler OnAnyGameSaved;

        public static void ResetStaticData() {
            OnAnySaveDataLoaded = null;
            OnAnyGameSaved = null;
        }

        public class OnSaveDataLoadedEventArgs : EventArgs {
            public GameSaveData saveData;
        }

        [System.Serializable]
        public class LevelProgressData {
            public int levelIndex;
            public string levelName;
            public bool isUnlocked;
            public int bestStars;          // 0 to 3 stars
            public int bestDailyRevenue;   // Best profit achieved in a single day
            public int totalDishesServed;
        }

        [System.Serializable]
        public class GameSaveData {
            public int saveVersion = 1;
            public int careerSavings = 100;
            public int lastPlayedLevelIndex = 0;
            public ulong purchasedUpgradesMask = 0;
            public int totalDaysCompleted = 0;
            public string lastSavedTimestamp = "";
            public List<LevelProgressData> levelProgress = new List<LevelProgressData>();
        }

        private const string SAVE_FILE_NAME = "dishaster_savegame.json";
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        [SerializeField] private GameSaveData currentSaveData;
        public GameSaveData CurrentSaveData => currentSaveData;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadGame();
        }

        private void Start() {
            DayLoopManager.OnAnyDaySummaryCalculated += DayLoopManager_OnAnyDaySummaryCalculated;
            NightMarketManager.OnAnyUpgradePurchased += NightMarketManager_OnAnyUpgradePurchased;

            if (KitchenGameManager.Instance != null) {
                KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
            }
        }

        private void OnDestroy() {
            DayLoopManager.OnAnyDaySummaryCalculated -= DayLoopManager_OnAnyDaySummaryCalculated;
            NightMarketManager.OnAnyUpgradePurchased -= NightMarketManager_OnAnyUpgradePurchased;

            if (KitchenGameManager.Instance != null) {
                KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
            }

            if (Instance == this) {
                Instance = null;
            }
        }

        private void KitchenGameManager_OnStateChanged(object sender, EventArgs e) {
            if (KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsCountdownToStartActive()) {
                ApplySaveToActiveGame();
            }
        }

        private void DayLoopManager_OnAnyDaySummaryCalculated(object sender, DayLoopManager.OnDaySummaryCalculatedEventArgs e) {
            // Only server/host persists campaign career earnings
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer) {
                return;
            }

            int activeLevelIndex = 0;
            if (LevelSelectionManager.Instance != null) {
                activeLevelIndex = LevelSelectionManager.Instance.ActiveLevelIndex;
            }

            RecordDayCompleted(activeLevelIndex, e.dayNumber, e.netProfit, e.starsAwarded, e.dishesDelivered);
        }

        private void NightMarketManager_OnAnyUpgradePurchased(object sender, NightMarketManager.OnUpgradePurchasedEventArgs e) {
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer) {
                return;
            }

            if (NightMarketManager.Instance != null) {
                currentSaveData.purchasedUpgradesMask = NightMarketManager.Instance.GetPurchasedUpgradesMask();
                SaveGame();
            }
        }

        /// <summary>
        /// Loads campaign progress from local JSON storage. If no save file exists, default profile is initialized.
        /// </summary>
        public void LoadGame() {
            try {
                if (File.Exists(SaveFilePath)) {
                    string json = File.ReadAllText(SaveFilePath);
                    currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                    Debug.Log($"[GameSaveManager] Successfully loaded campaign save from: {SaveFilePath}");
                } else {
                    Debug.Log("[GameSaveManager] No existing save found. Creating new career profile.");
                    CreateDefaultSaveData();
                    SaveGame();
                }
            } catch (Exception ex) {
                Debug.LogError($"[GameSaveManager] Failed to load save file: {ex.Message}. Initializing default data.");
                CreateDefaultSaveData();
            }

            ValidateLevelsList();
            OnAnySaveDataLoaded?.Invoke(this, new OnSaveDataLoadedEventArgs { saveData = currentSaveData });
        }

        /// <summary>
        /// Writes campaign progress atomically to local JSON storage.
        /// </summary>
        public void SaveGame() {
            try {
                currentSaveData.lastSavedTimestamp = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(currentSaveData, true);

                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(SaveFilePath)) {
                    File.Delete(SaveFilePath);
                }
                File.Move(tempPath, SaveFilePath);

                Debug.Log($"[GameSaveManager] Game saved successfully ({currentSaveData.careerSavings} career savings, {currentSaveData.totalDaysCompleted} days).");
                OnAnyGameSaved?.Invoke(this, EventArgs.Empty);
            } catch (Exception ex) {
                Debug.LogError($"[GameSaveManager] Failed to save game: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets career progress to brand-new default state.
        /// </summary>
        public void DeleteSave() {
            try {
                if (File.Exists(SaveFilePath)) {
                    File.Delete(SaveFilePath);
                }
                CreateDefaultSaveData();
                SaveGame();
                Debug.Log("[GameSaveManager] Save data deleted and reinitialized to defaults.");
            } catch (Exception ex) {
                Debug.LogError($"[GameSaveManager] Error deleting save: {ex.Message}");
            }
        }

        private void CreateDefaultSaveData() {
            currentSaveData = new GameSaveData {
                saveVersion = 1,
                careerSavings = 100,
                lastPlayedLevelIndex = 0,
                purchasedUpgradesMask = 0,
                totalDaysCompleted = 0,
                lastSavedTimestamp = DateTime.UtcNow.ToString("o"),
                levelProgress = new List<LevelProgressData>()
            };

            ValidateLevelsList();
        }

        private void ValidateLevelsList() {
            if (currentSaveData.levelProgress == null) {
                currentSaveData.levelProgress = new List<LevelProgressData>();
            }

            string[] defaultLevelNames = new string[] {
                "The Sunny Side Diner",
                "Downtown Burger Bar",
                "Boardwalk Grill"
            };

            for (int i = 0; i < defaultLevelNames.Length; i++) {
                LevelProgressData data = currentSaveData.levelProgress.Find(l => l.levelIndex == i);
                if (data == null) {
                    data = new LevelProgressData {
                        levelIndex = i,
                        levelName = defaultLevelNames[i],
                        isUnlocked = (i == 0), // Level 1 is unlocked initially
                        bestStars = 0,
                        bestDailyRevenue = 0,
                        totalDishesServed = 0
                    };
                    currentSaveData.levelProgress.Add(data);
                }
            }
        }

        /// <summary>
        /// Updates campaign progress after completing a day shift.
        /// </summary>
        public void RecordDayCompleted(int levelIndex, int dayNumber, int netProfit, int stars, int dishesDelivered) {
            currentSaveData.totalDaysCompleted++;
            if (netProfit > 0) {
                currentSaveData.careerSavings += netProfit;
            }

            LevelProgressData levelData = currentSaveData.levelProgress.Find(l => l.levelIndex == levelIndex);
            if (levelData != null) {
                if (stars > levelData.bestStars) {
                    levelData.bestStars = stars;
                }
                if (netProfit > levelData.bestDailyRevenue) {
                    levelData.bestDailyRevenue = netProfit;
                }
                levelData.totalDishesServed += dishesDelivered;
            }

            // Progression Unlock Rules:
            // Completing Level 0 with >= 1 star unlocks Level 1 (Downtown Burger Bar)
            if (levelIndex == 0 && stars >= 1) {
                UnlockLevel(1);
            }
            // Completing Level 1 with >= 2 stars unlocks Level 2 (Boardwalk Grill)
            if (levelIndex == 1 && stars >= 2) {
                UnlockLevel(2);
            }

            SaveGame();
        }

        public void UnlockLevel(int levelIndex) {
            LevelProgressData levelData = currentSaveData.levelProgress.Find(l => l.levelIndex == levelIndex);
            if (levelData != null && !levelData.isUnlocked) {
                levelData.isUnlocked = true;
                Debug.Log($"[GameSaveManager] 🌟 Unlocked Level {levelIndex + 1}: {levelData.levelName}!");
                SaveGame();
            }
        }

        public bool IsLevelUnlocked(int levelIndex) {
            LevelProgressData levelData = currentSaveData.levelProgress.Find(l => l.levelIndex == levelIndex);
            return levelData != null && levelData.isUnlocked;
        }

        public int GetLevelStars(int levelIndex) {
            LevelProgressData levelData = currentSaveData.levelProgress.Find(l => l.levelIndex == levelIndex);
            return levelData != null ? levelData.bestStars : 0;
        }

        /// <summary>
        /// Applies loaded upgrades and balance to active game managers on the server.
        /// </summary>
        public void ApplySaveToActiveGame() {
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer) {
                return;
            }

            if (NightMarketManager.Instance != null && currentSaveData.purchasedUpgradesMask != 0) {
                NightMarketManager.Instance.SetPurchasedUpgradesMaskServer(currentSaveData.purchasedUpgradesMask);
                Debug.Log($"[GameSaveManager] Applied saved Night Market upgrades (Mask: 0x{currentSaveData.purchasedUpgradesMask:X})");
            }

            if (RestaurantEconomyManager.Instance != null && currentSaveData.careerSavings > 100) {
                RestaurantEconomyManager.Instance.SetStartingBalanceServer(currentSaveData.careerSavings);
                Debug.Log($"[GameSaveManager] Restored career bank balance: ${currentSaveData.careerSavings}");
            }
        }
    }
}
