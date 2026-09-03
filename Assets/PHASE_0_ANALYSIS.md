# Phase 0: Preparation & Analysis

**Date Started:** September 2, 2026  
**Status:** In Progress

## Backup Status
✅ Created backup directory: `Assets/BACKUP_DeliverySystem/`
✅ Backed up files:
- DeliveryManager.cs
- DeliveryCounter.cs
- DeliveryManagerUI.cs
- DeliveryManagerSingleUI.cs

## Current System Overview

### Existing Files
- **DeliveryManager.cs** - Spawns recipes every 4s, validates deliveries
- **DeliveryCounter.cs** - Single delivery interaction point
- **DeliveryManagerUI.cs** - Shows all waiting recipes in UI list
- **DeliveryManagerSingleUI.cs** - Individual recipe display template
- **RecipeSO.cs** - Recipe definition (ingredients + name)
- **RecipeListSO.cs** - Collection of all recipes

### Key Parameters (Current)
- Max concurrent orders: 4
- Order spawn interval: 4 seconds
- Order spawning: Random from RecipeListSO
- Validation: Exact ingredient match (order-independent)
- Score tracking: Successful deliveries count only
- No penalties for wrong deliveries (order just doesn't complete)

## Dependency Analysis

### Files that reference DeliveryManager.Instance
1. **SoundManager.cs** - Lines 28-29
   - Subscribes to `OnRecipeSuccess` and `OnRecipeFailed` events
   - Used for audio feedback on deliveries
   - **Impact:** Keep events, TableManager will trigger them
   
2. **GameOverUI.cs** - Line 32
   - Calls `GetSuccessfulRecipesAmount()` to display score
   - **Impact:** TableManager needs to provide this method
   
3. **DeliveryResultUI.cs** - Lines 29-30
   - Subscribes to `OnRecipeSuccess` and `OnRecipeFailed` events
   - Shows success/failure popup animations
   - **Impact:** Keep events, TableManager will trigger them
   
4. **DeliveryManagerUI.cs** - Lines 17-18, 37
   - Subscribes to `OnRecipeSpawned` and `OnRecipeCompleted` events
   - Calls `GetWaitingRecipeSOList()` to display orders
   - **Impact:** Will be REPLACED by TableOrderUI system

### Files that reference DeliveryCounter.Instance
1. **DeliveryCounter.cs** - Line 12
   - Sets singleton instance
   - **Impact:** Counter will be removed/replaced by CustomerTable

### Event Dependencies (CRITICAL - Must Preserve)
These events are used by multiple systems and MUST remain functional:
- ✅ `OnRecipeSuccess` - Used by SoundManager, DeliveryResultUI
- ✅ `OnRecipeFailed` - Used by SoundManager, DeliveryResultUI
- ✅ `OnRecipeCompleted` - Used by DeliveryManagerUI (will be replaced)
- ✅ `OnRecipeSpawned` - Used by DeliveryManagerUI (will be replaced)

## Git Status
- Branch: `main`
- Uncommitted changes: Multiple modified files
- Next step: Create feature branch after committing current work or stashing

## Technical Requirements for Table System

### Network Synchronization Needs
1. Table order assignments must sync across all clients
2. Table numbers must be consistent
3. Delivery validation must be server-authoritative
4. Visual feedback must trigger on all clients

### Scene Requirements
1. 4-6 CustomerTable prefabs in GameScene
2. Each table needs:
   - Unique table number (1-6)
   - World-space canvas for order UI
   - NetworkObject component
   - Interaction collider
   - Visual feedback components

### Prefab Requirements
1. CustomerTable prefab with all components
2. TableOrderUI canvas prefab
3. Materials for visual states (normal, has-order, correct, wrong, selected)

## Migration Strategy

### Keep Unchanged
- ✅ DeliveryManager events (OnRecipeSuccess, OnRecipeFailed, OnRecipeCompleted, OnRecipeSpawned)
- ✅ SoundManager (no changes needed)
- ✅ DeliveryResultUI (no changes needed)
- ✅ GameOverUI (minimal change - call TableManager instead of DeliveryManager)

### Replace Completely
- ❌ DeliveryCounter.cs → CustomerTable.cs (new interaction logic)
- ❌ DeliveryManagerUI.cs → TableOrderUI.cs per table (distributed UI)
- ❌ DeliveryManagerSingleUI.cs → Integrated into TableOrderUI.cs

### Modify/Extend
- 🔄 DeliveryManager.cs - Keep as event hub, delegate logic to TableManager
- 🔄 GameOverUI.cs - Change line 32 to call TableManager

## Next Steps
1. ✅ Create backups
2. ✅ Analyze dependencies (complete)
3. ⏳ Create development branch
4. ⏳ Wait for user approval to proceed to Phase 1

## Risk Assessment
- **Low Risk:** Core cooking mechanics unchanged
- **Medium Risk:** Network synchronization of table states
- **Low Risk:** Can run both old and new systems during testing
- **Rollback Plan:** Restore from backup, revert git commits
