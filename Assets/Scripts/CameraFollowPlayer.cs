using System;
using UnityEngine;
using Cinemachine;

public class CameraFollowPlayer : MonoBehaviour {

    [Header("Camera Offset Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0.75f, 21.5f, -20.79f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(46f, 0f, 0f);

    [Header("Optional: Cinemachine (if using)")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private bool useCinemachine = true; // Default to true since Virtual Camera exists

    private GameObject cameraTarget;

    private void Awake() {
        // Check if CinemachineBrain is active
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain != null && brain.enabled && !useCinemachine) {
            Debug.LogWarning("CameraFollowPlayer: CinemachineBrain detected but useCinemachine is false. Enable useCinemachine and assign Virtual Camera.");
        }

        // Set initial camera rotation
        transform.rotation = Quaternion.Euler(cameraRotation);

        // Try to find virtual camera if using Cinemachine
        if (useCinemachine && virtualCamera == null) {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera != null) {
                Debug.Log($"CameraFollowPlayer: Auto-found Virtual Camera: {virtualCamera.name}");
            }
        }
    }

    private void Start() {
        if (Player.LocalInstance != null) {
            SetCameraTarget();
        } else {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }
    }

    private void Player_OnAnyPlayerSpawned(object sender, EventArgs e) {
        if (Player.LocalInstance != null) {
            SetCameraTarget();
            Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
        }
    }

    private void SetCameraTarget() {
        if (useCinemachine && virtualCamera != null) {
            // Cinemachine path: create intermediate target at desired camera height
            cameraTarget = new GameObject("CameraTarget");
            Vector3 playerPos = Player.LocalInstance.transform.position;
            cameraTarget.transform.position = playerPos + cameraOffset;
            virtualCamera.Follow = cameraTarget.transform;
            virtualCamera.LookAt = Player.LocalInstance.transform;
            Debug.Log($"CameraFollowPlayer: Set Virtual Camera to follow {Player.LocalInstance.name} via CameraTarget with offset {cameraOffset}");
        } else if (useCinemachine && virtualCamera == null) {
            Debug.LogError("CameraFollowPlayer: useCinemachine is true but no Virtual Camera assigned or found!");
        }
        // If not using Cinemachine, LateUpdate will handle it directly
    }

    private void LateUpdate() {
        if (Player.LocalInstance == null) return;

        Vector3 playerPos = Player.LocalInstance.transform.position;

        if (useCinemachine && cameraTarget != null) {
            // Update Cinemachine target position with offset
            cameraTarget.transform.position = playerPos + cameraOffset;
        } else if (!useCinemachine) {
            // Direct camera control: follow player with offset
            transform.position = playerPos + cameraOffset;
            // Look at player
            transform.LookAt(playerPos);
        }
    }

    private void OnDestroy() {
        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;

        if (cameraTarget != null) {
            Destroy(cameraTarget);
        }
    }

}
