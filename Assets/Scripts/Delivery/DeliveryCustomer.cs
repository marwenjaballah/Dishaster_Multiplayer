using System;
using System.Collections;
using UnityEngine;
using CityLife;

/// <summary>
/// A delivery customer NPC that:
///   1. Stands still at a sidewalk position waiting for a delivery.
///   2. Shows TableOrderUI (identical to Dining Tables) above their head with order info, icons, and patience timer.
///   3. When the local player is within interaction range and presses E,
///      the DeliveryCustomerManager validates and completes the order.
/// </summary>
[RequireComponent(typeof(PedestrianController))]
public class DeliveryCustomer : MonoBehaviour
{
    // ── Public Events ─────────────────────────────────────────────────────────
    public event Action<DeliveryCustomer> OnDeliverySucceeded;
    public event Action<DeliveryCustomer> OnDeliveryExpired;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Order UI (From EggTable)")]
    [SerializeField] private TableOrderUI tableOrderUI;

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 3f;

    // ── Runtime State ─────────────────────────────────────────────────────────
    private int        _deliveryId;
    private RecipeSO   _assignedRecipe;
    private float      _timerRemaining;
    private bool       _active;
    private bool       _playerInRange;
    private float      _orderTimerMax;

    private PedestrianController _ped;

    public int GetDeliveryId() => _deliveryId;
    public void SetDeliveryId(int id) => _deliveryId = id;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ped = GetComponent<PedestrianController>();
        if (tableOrderUI == null)
        {
            tableOrderUI = GetComponentInChildren<TableOrderUI>(true);
        }
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
        _active = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ActivateAsCustomer(RecipeSO recipe, Vector3 sidewalkPosition, float orderTimer)
    {
        _assignedRecipe = recipe;
        _timerRemaining = orderTimer;
        _orderTimerMax  = orderTimer;
        _active         = true;

        transform.position = sidewalkPosition;
        var lookDir = (Vector3.zero - sidewalkPosition);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        StopPedestrianWalking();

        if (tableOrderUI != null)
        {
            tableOrderUI.gameObject.SetActive(true);
            tableOrderUI.ShowOrder(recipe, _deliveryId);
            tableOrderUI.UpdateTimerBar(1f);
        }
    }

    public RecipeSO GetRecipe() => _assignedRecipe;
    public bool IsActive() => _active;
    public float GetRemainingTime() => _timerRemaining;
    public float GetOrderTimerMax() => _orderTimerMax;
    public float GetPatienceNormalized() => _orderTimerMax > 0f ? Mathf.Clamp01(_timerRemaining / _orderTimerMax) : 0f;

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        _timerRemaining -= Time.deltaTime;

        if (tableOrderUI != null)
        {
            tableOrderUI.UpdateTimerBar(GetPatienceNormalized());
        }

        if (_timerRemaining <= 0f)
        {
            ExpireOrder();
            return;
        }

        _playerInRange = IsPlayerInRange();
    }

    private bool IsPlayerInRange()
    {
        if (Player.LocalInstance == null) return false;
        float sqrDist = (Player.LocalInstance.transform.position - transform.position).sqrMagnitude;
        return sqrDist <= interactRadius * interactRadius;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (!_active || !_playerInRange) return;
        if (Player.LocalInstance == null) return;

        DeliveryCustomerManager.Instance.TryDeliverToCustomer(this, Player.LocalInstance);
    }

    public void CompleteDelivery(int totalEarned = 0)
    {
        _active = false;
        if (tableOrderUI != null)
        {
            tableOrderUI.HideOrder();
        }
        OnDeliverySucceeded?.Invoke(this);
        StartCoroutine(ShowResultThenDeactivate(true));
    }

    public void FlashWrongDeliveryFeedback()
    {
        if (!_active) return;
        if (tableOrderUI != null)
        {
            tableOrderUI.ShowDeliveryResult(false);
        }
    }

    private void ExpireOrder()
    {
        _active = false;
        if (tableOrderUI != null)
        {
            tableOrderUI.HideOrder();
        }
        OnDeliveryExpired?.Invoke(this);
        StartCoroutine(ShowResultThenDeactivate(false));
    }

    private IEnumerator ShowResultThenDeactivate(bool success)
    {
        if (tableOrderUI != null)
        {
            tableOrderUI.ShowDeliveryResult(success);
        }

        yield return new WaitForSeconds(1.5f);

        if (tableOrderUI != null)
        {
            tableOrderUI.HideOrder();
        }

        gameObject.SetActive(false);
    }

    private void StopPedestrianWalking()
    {
        if (_ped != null)
            _ped.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
