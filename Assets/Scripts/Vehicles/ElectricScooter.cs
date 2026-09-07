using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Vehicles
{
    /// <summary>
    /// An Electric Scooter (Trottinette) vehicle.
    /// Parked outside the kitchen exit, allowing players to mount/dismount with [F].
    /// Features increased movement speed (16f), responsive steering, and audio.
    /// </summary>
    public class ElectricScooter : NetworkBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Vehicle Settings")]
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float mountDistance = 2.5f;
        [SerializeField] private LayerMask collisionsLayerMask;

        [Header("References")]
        [SerializeField] private Transform playerMountPoint;
        [SerializeField] private GameObject interactPromptUI; // "[F] Ride Scooter"
        [SerializeField] private AudioSource motorAudioSource;

        // ── Network State ─────────────────────────────────────────────────────────
        // ulong.MaxValue means nobody is riding
        private readonly NetworkVariable<ulong> _riderClientId = new(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ── Local State ───────────────────────────────────────────────────────────
        private Player _localRider;
        private bool _isLocalMounted;
        private Vector3 _currentMoveDir;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _riderClientId.OnValueChanged += OnRiderClientIdChanged;
            UpdateMountVisuals(_riderClientId.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _riderClientId.OnValueChanged -= OnRiderClientIdChanged;
        }

        private void OnEnable()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
            }
        }

        private void OnDisable()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.OnInteractAlternateAction -= GameInput_OnInteractAlternateAction;
            }
        }

        private void Start()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.OnInteractAlternateAction -= GameInput_OnInteractAlternateAction;
                GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
            }
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }

        // ── Input & Interactions ──────────────────────────────────────────────────

        private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
        {
            if (!KitchenGameManager.Instance.IsGamePlaying()) return;

            if (_isLocalMounted)
            {
                // Request dismount
                RequestDismountServerRpc();
            }
            else
            {
                // Check if unmounted and local player is close enough
                if (_riderClientId.Value == ulong.MaxValue && IsLocalPlayerNearby())
                {
                    RequestMountServerRpc();
                }
            }
        }

        private bool IsLocalPlayerNearby()
        {
            if (Player.LocalInstance == null) return false;
            float sqrDist = (Player.LocalInstance.transform.position - transform.position).sqrMagnitude;
            return sqrDist <= mountDistance * mountDistance;
        }

        // ── Server RPCs ───────────────────────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        private void RequestMountServerRpc(ServerRpcParams rpcParams = default)
        {
            if (_riderClientId.Value != ulong.MaxValue) return; // Already occupied

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            _riderClientId.Value = senderClientId;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDismountServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (_riderClientId.Value == senderClientId)
            {
                _riderClientId.Value = ulong.MaxValue;
            }
        }

        // ── State Handlers ────────────────────────────────────────────────────────

        private void OnRiderClientIdChanged(ulong previousValue, ulong newValue)
        {
            UpdateMountVisuals(newValue);
        }

        private void UpdateMountVisuals(ulong riderId)
        {
            if (riderId == ulong.MaxValue)
            {
                // Dismounted
                if (_localRider != null)
                {
                    _localRider.enabled = true;
                    var col = _localRider.GetComponent<Collider>();
                    if (col != null) col.enabled = true;
                    _localRider = null;
                }
                _isLocalMounted = false;
                if (motorAudioSource != null && motorAudioSource.isPlaying) motorAudioSource.Stop();
            }
            else
            {
                // Mounted
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == riderId)
                {
                    _isLocalMounted = true;
                    _localRider = Player.LocalInstance;

                    if (_localRider != null)
                    {
                        var col = _localRider.GetComponent<Collider>();
                        if (col != null) col.enabled = false;
                        _localRider.enabled = false; // Disable standard player movement script
                        
                        Vector3 mountPos = playerMountPoint != null ? playerMountPoint.position : transform.position;
                        _localRider.transform.position = mountPos;
                        _localRider.transform.rotation = transform.rotation;
                    }

                    if (motorAudioSource != null && !motorAudioSource.isPlaying) motorAudioSource.Play();
                }
            }
        }

        private void LateUpdate()
        {
            // Keep rider clamped to mount point without altering NetworkObject hierarchy
            if (_isLocalMounted && _localRider != null)
            {
                Vector3 mountPos = playerMountPoint != null ? playerMountPoint.position : transform.position;
                _localRider.transform.position = mountPos;
                _localRider.transform.rotation = transform.rotation;
            }
        }

        // ── Driving Loop ──────────────────────────────────────────────────────────

        private void Update()
        {
            // Prompt display
            if (!_isLocalMounted && interactPromptUI != null)
            {
                bool show = _riderClientId.Value == ulong.MaxValue && IsLocalPlayerNearby();
                if (interactPromptUI.activeSelf != show) interactPromptUI.SetActive(show);
            }

            if (!_isLocalMounted) return;

            // Handle driving input for local rider
            HandleDriving();
        }

        private void HandleDriving()
        {
            Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
            Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

            float moveDistance = moveSpeed * Time.deltaTime;
            float vehicleRadius = 0.5f;
            float vehicleHeight = 1.0f;
            Vector3 boxCenter = transform.position + Vector3.up * 0.5f;
            Vector3 halfExtents = new Vector3(vehicleRadius, vehicleHeight / 2f, vehicleRadius);

            bool isMoving = moveDir != Vector3.zero;

            if (isMoving)
            {
                bool canMove = !Physics.BoxCast(boxCenter, halfExtents, moveDir, Quaternion.identity, moveDistance, collisionsLayerMask, QueryTriggerInteraction.Ignore);

                if (!canMove)
                {
                    // Attempt X only
                    Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
                    canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.BoxCast(boxCenter, halfExtents, moveDirX, Quaternion.identity, moveDistance, collisionsLayerMask, QueryTriggerInteraction.Ignore);
                    if (canMove) moveDir = moveDirX;
                    else
                    {
                        // Attempt Z only
                        Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                        canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.BoxCast(boxCenter, halfExtents, moveDirZ, Quaternion.identity, moveDistance, collisionsLayerMask, QueryTriggerInteraction.Ignore);
                        if (canMove) moveDir = moveDirZ;
                        else moveDir = Vector3.zero;
                    }
                }

                if (moveDir != Vector3.zero)
                {
                    transform.position += moveDir * moveDistance;
                    transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
                }

                // Sync position to server if owner or rider
                UpdateServerTransformServerRpc(transform.position, transform.rotation);
            }

            // Keep rider clamped to mount point
            if (_localRider != null && playerMountPoint != null)
            {
                _localRider.transform.position = playerMountPoint.position;
                _localRider.transform.rotation = transform.rotation;
            }

            // Audio pitch modulation
            if (motorAudioSource != null)
            {
                motorAudioSource.pitch = Mathf.Lerp(motorAudioSource.pitch, isMoving ? 1.4f : 0.8f, Time.deltaTime * 5f);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateServerTransformServerRpc(Vector3 pos, Quaternion rot)
        {
            UpdateClientTransformClientRpc(pos, rot);
        }

        [ClientRpc]
        private void UpdateClientTransformClientRpc(Vector3 pos, Quaternion rot)
        {
            if (_isLocalMounted) return; // Local driver already updated locally
            transform.position = pos;
            transform.rotation = rot;
        }

        public bool IsMounted() => _riderClientId.Value != ulong.MaxValue;
    }
}
