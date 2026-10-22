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

        public static void ResetStaticData()
        {
            Instance = null;
        }

        private TextMesh _distanceText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (visualContainer == null)
            {
                CreateProceduralVisuals();
            }

            if (arrowRenderer != null)
            {
                _indicatorMaterial = arrowRenderer.material;
            }
        }

        private void CreateProceduralVisuals()
        {
            visualContainer = new GameObject("ArrowVisualContainer");
            visualContainer.transform.SetParent(transform, false);

            // 1. Sleek Neon Directional Chevron Arrow (pointing forward along Z+)
            GameObject arrowShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowShaft.name = "ArrowShaft";
            arrowShaft.transform.SetParent(visualContainer.transform, false);
            arrowShaft.transform.localPosition = new Vector3(0f, 0f, 0f);
            arrowShaft.transform.localScale = new Vector3(0.12f, 0.08f, 0.50f);
            Destroy(arrowShaft.GetComponent<Collider>());

            GameObject arrowHeadL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowHeadL.name = "ArrowHead_Left";
            arrowHeadL.transform.SetParent(visualContainer.transform, false);
            arrowHeadL.transform.localPosition = new Vector3(-0.14f, 0f, 0.22f);
            arrowHeadL.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
            arrowHeadL.transform.localScale = new Vector3(0.10f, 0.08f, 0.35f);
            Destroy(arrowHeadL.GetComponent<Collider>());

            GameObject arrowHeadR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowHeadR.name = "ArrowHead_Right";
            arrowHeadR.transform.SetParent(visualContainer.transform, false);
            arrowHeadR.transform.localPosition = new Vector3(0.14f, 0f, 0.22f);
            arrowHeadR.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
            arrowHeadR.transform.localScale = new Vector3(0.10f, 0.08f, 0.35f);
            Destroy(arrowHeadR.GetComponent<Collider>());

            Material neonMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            neonMat.color = normalColor;
            neonMat.EnableKeyword("_EMISSION");
            neonMat.SetColor("_EmissionColor", normalColor * 0.8f);

            var rShaft = arrowShaft.GetComponent<MeshRenderer>();
            var rLeft = arrowHeadL.GetComponent<MeshRenderer>();
            var rRight = arrowHeadR.GetComponent<MeshRenderer>();

            if (rShaft != null) rShaft.material = neonMat;
            if (rLeft != null) rLeft.material = neonMat;
            if (rRight != null) rRight.material = neonMat;

            arrowRenderer = rShaft;
            _indicatorMaterial = neonMat;

            // 2. Floating Distance Readout
            GameObject textObj = new GameObject("DistanceLabel");
            textObj.transform.SetParent(visualContainer.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            textObj.transform.localScale = Vector3.one * 0.14f;

            _distanceText = textObj.AddComponent<TextMesh>();
            _distanceText.alignment = TextAlignment.Center;
            _distanceText.anchor = TextAnchor.MiddleCenter;
            _distanceText.fontSize = 24;
            _distanceText.fontStyle = FontStyle.Bold;
            _distanceText.color = Color.white;
            _distanceText.text = "";

            visualContainer.SetActive(false);
        }

        private void Update()
        {
            if (Player.LocalInstance == null)
            {
                SetVisible(false);
                return;
            }

            // Show 3D arrow whenever carrying delivery items or driving the scooter
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
            if (player.IsMountedOnVehicle()) return true;
            if (!player.HasKitchenObject()) return false;
            var obj = player.GetKitchenObject();
            return obj is DeliveryBagKitchenObject || obj is PlateKitchenObject;
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

            float dist = toTarget.magnitude;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationTurnSpeed);
            }

            // 3. Proximity color & distance label
            Color activeCol = dist <= 8f ? nearColor : normalColor;
            if (_indicatorMaterial != null)
            {
                _indicatorMaterial.color = activeCol;
                if (_indicatorMaterial.HasProperty("_EmissionColor"))
                {
                    _indicatorMaterial.SetColor("_EmissionColor", activeCol * 0.8f);
                }
            }

            if (_distanceText != null)
            {
                _distanceText.text = $"{dist:F0}m";
                _distanceText.color = activeCol;
                if (Camera.main != null)
                {
                    // Face billboard camera
                    _distanceText.transform.rotation = Camera.main.transform.rotation;
                }
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
