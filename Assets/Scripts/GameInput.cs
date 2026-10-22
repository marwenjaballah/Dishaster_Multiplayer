using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {


    private const string PLAYER_PREFS_BINDINGS = "InputBindings";


    private static GameInput instance;
    public static GameInput Instance {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<GameInput>();
                if (instance == null) {
                    GameObject gameInputGo = new GameObject("GameInput");
                    instance = gameInputGo.AddComponent<GameInput>();
                }
            }
            return instance;
        }
        private set {
            instance = value;
        }
    }

    public enum DeviceType {
        KeyboardMouse,
        XboxGamepad,
        PlayStationGamepad,
        SteamDeck,
        GenericGamepad,
        TouchScreen
    }

    public static event EventHandler<DeviceType> OnActiveDeviceChanged;

    public static void ResetStaticData() {
        OnActiveDeviceChanged = null;
    }

    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebind;


    public enum Binding {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause
    }


    private PlayerInputActions playerInputActions;
    private DeviceType currentDeviceType = DeviceType.KeyboardMouse;
    public DeviceType CurrentDeviceType => currentDeviceType;
    private Action<object, InputActionChange> onActionChangeDelegate;


    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;


        playerInputActions = new PlayerInputActions();

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS)) {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        playerInputActions.Player.Enable();

        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.InteractAlternate.performed += InteractAlternate_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;

        onActionChangeDelegate = (obj, change) => {
            if (change == InputActionChange.ActionPerformed) {
                InputAction action = obj as InputAction;
                if (action != null && action.activeControl != null) {
                    UpdateActiveDevice(action.activeControl.device);
                }
            }
        };
        InputSystem.onActionChange += onActionChangeDelegate;

        EnsureMobileControls();
    }

    private void UpdateActiveDevice(InputDevice device) {
        if (device == null) return;

        DeviceType newType = currentDeviceType;

        if (device is Gamepad gamepad) {
            string deviceName = (gamepad.displayName + " " + gamepad.description.product).ToLowerInvariant();

            if (deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || deviceName.Contains("playstation") || deviceName.Contains("sony") || deviceName.Contains("wireless controller")) {
                newType = DeviceType.PlayStationGamepad;
            } else if (deviceName.Contains("deck") || SystemInfo.deviceModel.ToLowerInvariant().Contains("steam deck")) {
                newType = DeviceType.SteamDeck;
            } else if (deviceName.Contains("xbox") || deviceName.Contains("xinput")) {
                newType = DeviceType.XboxGamepad;
            } else {
                newType = DeviceType.GenericGamepad;
            }
        } else if (device is Touchscreen) {
            newType = DeviceType.TouchScreen;
        } else if (device is Keyboard || device is Mouse) {
            newType = DeviceType.KeyboardMouse;
        }

        if (newType != currentDeviceType) {
            currentDeviceType = newType;
            OnActiveDeviceChanged?.Invoke(this, currentDeviceType);
        }
    }

    private void EnsureMobileControls() {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == Loader.Scene.GameScene.ToString() || sceneName == "GameSceneTableService") {
            if (FindAnyObjectByType<UI.MobileTouchControlsUI>() == null) {
                GameObject mobileControlsGo = new GameObject("MobileTouchControlsUI");
                mobileControlsGo.AddComponent<UI.MobileTouchControlsUI>();
            }
        }
    }

    private void OnDestroy() {
        if (onActionChangeDelegate != null) {
            InputSystem.onActionChange -= onActionChangeDelegate;
            onActionChangeDelegate = null;
        }

        if (playerInputActions != null) {
            playerInputActions.Player.Interact.performed -= Interact_performed;
            playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
            playerInputActions.Player.Pause.performed -= Pause_performed;

            playerInputActions.Dispose();
        }
        if (instance == this) {
            instance = null;
        }
    }

    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void InteractAlternate_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    private Vector2 mobileMovementVector;
    private bool isMobileInteractPressed;
    private bool isMobileInteractAlternatePressed;

    public void SetMobileMovementVector(Vector2 moveVec) {
        mobileMovementVector = moveVec;
    }

    public void SetMobileInteractPressed(bool isPressed) {
        isMobileInteractPressed = isPressed;
    }

    public void SetMobileInteractAlternatePressed(bool isPressed) {
        isMobileInteractAlternatePressed = isPressed;
    }

    public void TriggerMobileInteract() {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerMobileInteractAlternate() {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerMobilePause() {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    public bool IsInteractPressed() {
        return isMobileInteractPressed || (playerInputActions != null && playerInputActions.Player.Interact.IsPressed());
    }

    public bool IsInteractAlternatePressed() {
        return isMobileInteractAlternatePressed || (playerInputActions != null && playerInputActions.Player.InteractAlternate.IsPressed());
    }

    public Vector2 GetMovementVectorNormalized() {
        Vector2 inputVector = playerInputActions != null ? playerInputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;

        if (mobileMovementVector.sqrMagnitude > 0.001f) {
            inputVector = mobileMovementVector;
        }

        inputVector = inputVector.normalized;

        return inputVector;
    }

    public string GetBindingText(Binding binding) {
        switch (binding) {
            default:
            case Binding.Move_Up:
                return playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return FormatGamepadGlyph(playerInputActions.Player.Interact.bindings[1].ToDisplayString(), Binding.Gamepad_Interact);
            case Binding.Gamepad_InteractAlternate:
                return FormatGamepadGlyph(playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString(), Binding.Gamepad_InteractAlternate);
            case Binding.Gamepad_Pause:
                return FormatGamepadGlyph(playerInputActions.Player.Pause.bindings[1].ToDisplayString(), Binding.Gamepad_Pause);
        }
    }

    private string FormatGamepadGlyph(string defaultString, Binding binding) {
        switch (currentDeviceType) {
            case DeviceType.PlayStationGamepad:
                if (binding == Binding.Gamepad_Interact) return "[X] Cross";
                if (binding == Binding.Gamepad_InteractAlternate) return "[Sqr] Square";
                if (binding == Binding.Gamepad_Pause) return "[Options]";
                break;
            case DeviceType.SteamDeck:
                if (binding == Binding.Gamepad_Interact) return "[A]";
                if (binding == Binding.Gamepad_InteractAlternate) return "[X]";
                if (binding == Binding.Gamepad_Pause) return "[Menu]";
                break;
            case DeviceType.XboxGamepad:
            case DeviceType.GenericGamepad:
                if (binding == Binding.Gamepad_Interact) return "[A]";
                if (binding == Binding.Gamepad_InteractAlternate) return "[X]";
                if (binding == Binding.Gamepad_Pause) return "[Menu]";
                break;
        }
        return defaultString;
    }

    public void RebindBinding(Binding binding, Action onActionRebound) {
        playerInputActions.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding) {
            default:
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }

}