using System.Collections.Generic;
using UnityEngine;

namespace Delivery
{
    /// <summary>
    /// A floating 3D in-world arrow that hovers directly ABOVE THE LOCAL PLAYER'S HEAD
    /// when the player is carrying a Delivery Bag. It rotates horizontally in 3D world space,
    /// pointing directly towards the closest active delivery customer.
    /// </summary>
    public class DeliveryTarget3DIndicator : MonoBehaviour
    {
        public static DeliveryTarget3DIndicator Instance { get; private set; }

        [Header("Hover Above Player Settings")]
        [SerializeField] private float heightAbovePlayer = 2.4f;
        [SerializeField] private float bobSpeed = 4.0f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float followLerpSpeed = 15f;
        [SerializeField] private float rotationTurnSpeed = 15f;

        [Header("Visuals")]
        [SerializeField] private GameObject visualContainer;
        [SerializeField] private Renderer arrowRenderer;
        [SerializeField] private Color normalColor = new Color(0.15f, 0.85f, 1f); // Cyan
        [SerializeField] private Color nearColor = new Color(0.2f, 1f, 0.4f);    // Green

        private DeliveryCustomer _currentTarget;
        private Material _indicatorMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (arrowRenderer != null)
            {
                _indicatorMaterial = arrowRenderer.material;
            }
        }

        private void Update()
        {
            if (Player.LocalInstance == null)
            {
                SetVisible(false);
                return;
            }

            // Only show 3D arrow above player when carrying a Delivery Bag (or a plate for delivery)
            bool isCarryingDelivery = IsPlayerCarryingDelivery(Player.LocalInstance);
            if (!isCarryingDelivery)
            {
                SetVisible(false);
                return;
            }

            _currentTarget = FindClosestActiveCustomer();
            if (_currentTarget == null || !_currentTarget.IsActive())
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            UpdateIndicatorPositionAndRotation();
        }

        private bool IsPlayerCarryingDelivery(Player player)
        {
            if (!player.HasKitchenObject()) return false;
            return player.GetKitchenObject() is DeliveryBagKitchenObject;
        }

        private DeliveryCustomer FindClosestActiveCustomer()
        {
            if (DeliveryCustomerManager.Instance == null) return null;

            List<DeliveryCustomer> list = DeliveryCustomerManager.Instance.GetActiveCustomers();
            if (list == null || list.Count == 0) return null;

            Vector3 playerPos = Player.LocalInstance.transform.position;
            DeliveryCustomer closest = null;
            float minDistSqr = float.MaxValue;

            foreach (var customer in list)
            {
                if (customer == null || !customer.IsActive()) continue;

                float distSqr = (customer.transform.position - playerPos).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closest = customer;
                }
            }

            return closest;
        }

        private void UpdateIndicatorPositionAndRotation()
        {
            Vector3 playerPos = Player.LocalInstance.transform.position;
            Vector3 targetPos = _currentTarget.transform.position;

            // 1. Follow directly above player's head with subtle vertical bobbing
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            Vector3 desiredPos = playerPos + Vector3.up * (heightAbovePlayer + bobOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followLerpSpeed);

            // 2. Rotate to point horizontally towards the target customer
            Vector3 toTarget = targetPos - playerPos;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationTurnSpeed);
            }

            // 3. Proximity color
            if (_indicatorMaterial != null)
            {
                float dist = toTarget.magnitude;
                _indicatorMaterial.color = dist <= 6f ? nearColor : normalColor;
            }
        }

        private void SetVisible(bool visible)
        {
            if (visualContainer != null && visualContainer.activeSelf != visible)
            {
                visualContainer.SetActive(visible);
            }
        }

        public DeliveryCustomer GetCurrentTarget() => _currentTarget;
    }
}
