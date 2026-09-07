using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Corner Mini-Map / Radar HUD that displays:
    /// 1. The central Restaurant location (Warm Gold).
    /// 2. All active delivery customer locations (Cyan / Gold closest).
    /// 3. The player's position relative to the restaurant and deliveries.
    /// </summary>
    public class DeliveryMinimapRadarUI : MonoBehaviour
    {
        [Header("Radar References")]
        [SerializeField] private RectTransform blipsContainer;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI nearestDistanceText;

        [Header("Settings")]
        [SerializeField] private float radarRadius = 60f;
        [SerializeField] private float radarWorldRange = 80f;
        [SerializeField] private float fadeSpeed = 6f;

        [Header("Restaurant Location")]
        [Tooltip("Optional transform for restaurant center. If null, auto-calculated from tables/counters.")]
        [SerializeField] private Transform restaurantTransform;
        [SerializeField] private Vector3 defaultRestaurantPosition = new Vector3(105f, 0f, -22f);
        [SerializeField] private Color restaurantBlipColor = new Color(1f, 0.75f, 0.15f, 1f); // Warm Gold

        [Header("Customer Colors")]
        [SerializeField] private Color normalBlipColor = new Color(0.2f, 0.85f, 1f, 0.9f); // Cyan
        [SerializeField] private Color closestBlipColor = new Color(1f, 0.85f, 0.1f, 1f);   // Gold
        [SerializeField] private Color nearBlipColor = new Color(0.2f, 1f, 0.35f, 1f);     // Green

        // Pool of customer blip UI objects
        private readonly List<RectTransform> _blipPool = new();
        private readonly List<Image> _blipImages = new();

        // Dedicated markers
        private RectTransform _restaurantMarker;
        private Image _restaurantMarkerImage;
        private RectTransform _playerCenterMarker;
        private Vector3 _cachedRestaurantPosition;
        private bool _hasCalculatedRestaurantPos;

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            EnsureDedicatedMarkers();
            CalculateRestaurantPosition();
        }

        private void EnsureDedicatedMarkers()
        {
            Transform parent = blipsContainer != null ? blipsContainer : transform;

            // 1. Restaurant Marker (Warm Gold Diamond/Square)
            if (_restaurantMarker == null)
            {
                var restObj = new GameObject("RestaurantMarker", typeof(RectTransform), typeof(Image));
                restObj.transform.SetParent(parent, false);

                _restaurantMarker = restObj.GetComponent<RectTransform>();
                _restaurantMarker.sizeDelta = new Vector2(14f, 14f);
                _restaurantMarker.anchorMin = new Vector2(0.5f, 0.5f);
                _restaurantMarker.anchorMax = new Vector2(0.5f, 0.5f);
                _restaurantMarker.pivot = new Vector2(0.5f, 0.5f);

                _restaurantMarkerImage = restObj.GetComponent<Image>();
                _restaurantMarkerImage.color = restaurantBlipColor;

                // Inner dot for restaurant visual flair
                var innerGo = new GameObject("InnerDot", typeof(RectTransform), typeof(Image));
                innerGo.transform.SetParent(restObj.transform, false);
                var innerRect = innerGo.GetComponent<RectTransform>();
                innerRect.sizeDelta = new Vector2(6f, 6f);
                innerRect.anchorMin = new Vector2(0.5f, 0.5f);
                innerRect.anchorMax = new Vector2(0.5f, 0.5f);
                innerRect.pivot = new Vector2(0.5f, 0.5f);
                innerGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            }

            // 2. Player Center Marker (White dot)
            if (_playerCenterMarker == null)
            {
                var playerObj = new GameObject("PlayerCenterMarker", typeof(RectTransform), typeof(Image));
                playerObj.transform.SetParent(parent, false);

                _playerCenterMarker = playerObj.GetComponent<RectTransform>();
                _playerCenterMarker.sizeDelta = new Vector2(6f, 6f);
                _playerCenterMarker.anchorMin = new Vector2(0.5f, 0.5f);
                _playerCenterMarker.anchorMax = new Vector2(0.5f, 0.5f);
                _playerCenterMarker.pivot = new Vector2(0.5f, 0.5f);
                _playerCenterMarker.anchoredPosition = Vector2.zero;

                var img = playerObj.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }

        private void CalculateRestaurantPosition()
        {
            if (restaurantTransform != null)
            {
                _cachedRestaurantPosition = restaurantTransform.position;
                _hasCalculatedRestaurantPos = true;
                return;
            }

            // Auto-detect center of dining tables & kitchen counters
            var tables = FindObjectsByType<CustomerTable>(FindObjectsSortMode.None);
            if (tables.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var t in tables) center += t.transform.position;
                _cachedRestaurantPosition = center / tables.Length;
                _hasCalculatedRestaurantPos = true;
                return;
            }

            var counters = FindObjectsByType<BaseCounter>(FindObjectsSortMode.None);
            if (counters.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var c in counters) center += c.transform.position;
                _cachedRestaurantPosition = center / counters.Length;
                _hasCalculatedRestaurantPos = true;
                return;
            }

            _cachedRestaurantPosition = defaultRestaurantPosition;
            _hasCalculatedRestaurantPos = true;
        }

        private float _lastTextUpdateTime;

        private void Update()
        {
            Transform camTr = LookAtCamera.MainCameraTransform;
            if (Player.LocalInstance == null || camTr == null)
            {
                SetVisible(false);
                return;
            }

            // Keep radar active when game is playing
            bool isPlaying = KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsGamePlaying();
            if (!isPlaying)
            {
                SetVisible(false);
                HideAllCustomerBlips();
                return;
            }

            SetVisible(true);
            EnsureDedicatedMarkers();

            Vector3 playerPos = Player.LocalInstance.transform.position;
            Vector3 camForward = camTr.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = camTr.right;
            camRight.y = 0f;
            camRight.Normalize();

            // 1. Update Restaurant Marker
            UpdateRestaurantMarker(playerPos, camForward, camRight);

            // 2. Update Customer Blips
            UpdateCustomerBlips(playerPos, camForward, camRight);
        }

        private void UpdateRestaurantMarker(Vector3 playerPos, Vector3 camForward, Vector3 camRight)
        {
            if (!_hasCalculatedRestaurantPos) CalculateRestaurantPosition();
            if (_restaurantMarker == null) return;

            Vector3 diff = _cachedRestaurantPosition - playerPos;
            float dist = diff.magnitude;

            float localX = Vector3.Dot(diff, camRight);
            float localY = Vector3.Dot(diff, camForward);

            Vector2 radarOffset = new Vector2(localX, localY) * (radarRadius / radarWorldRange);
            bool isClamped = radarOffset.magnitude > radarRadius;
            if (isClamped)
            {
                radarOffset = radarOffset.normalized * radarRadius;
            }

            _restaurantMarker.anchoredPosition = radarOffset;

            // Pulse restaurant marker slightly when far away so it's easy to spot
            float pulse = 1f + (isClamped ? Mathf.PingPong(Time.time * 2f, 0.2f) : 0f);
            _restaurantMarker.localScale = new Vector3(pulse, pulse, 1f);
            _restaurantMarker.gameObject.SetActive(true);
        }

        private void UpdateCustomerBlips(Vector3 playerPos, Vector3 camForward, Vector3 camRight)
        {
            List<DeliveryCustomer> activeCustomers = DeliveryCustomerManager.Instance != null
                ? DeliveryCustomerManager.Instance.GetActiveCustomers()
                : null;

            DeliveryCustomer closest = null;
            float minDist = float.MaxValue;

            int blipIndex = 0;
            if (activeCustomers != null)
            {
                // Find closest customer
                foreach (var c in activeCustomers)
                {
                    if (c == null || !c.IsActive()) continue;
                    float d = Vector3.Distance(playerPos, c.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = c;
                    }
                }

                // Render each customer blip
                foreach (var c in activeCustomers)
                {
                    if (c == null || !c.IsActive()) continue;

                    RectTransform blipRect = GetOrCreateBlip(blipIndex);
                    Image blipImg = _blipImages[blipIndex];

                    Vector3 diff = c.transform.position - playerPos;
                    float dist = diff.magnitude;

                    float localX = Vector3.Dot(diff, camRight);
                    float localY = Vector3.Dot(diff, camForward);

                    Vector2 radarOffset = new Vector2(localX, localY) * (radarRadius / radarWorldRange);
                    bool isClamped = radarOffset.magnitude > radarRadius;
                    if (isClamped)
                    {
                        radarOffset = radarOffset.normalized * radarRadius;
                    }

                    blipRect.anchoredPosition = radarOffset;

                    bool isClosest = (c == closest);
                    if (isClosest)
                    {
                        blipImg.color = dist <= 6f ? nearBlipColor : closestBlipColor;
                        float pulse = 1f + Mathf.PingPong(Time.time * 3f, 0.35f);
                        blipRect.localScale = new Vector3(pulse * 1.3f, pulse * 1.3f, 1f);
                        blipRect.SetAsLastSibling();
                    }
                    else
                    {
                        blipImg.color = normalBlipColor;
                        blipRect.localScale = Vector3.one;
                    }

                    blipRect.gameObject.SetActive(true);
                    blipIndex++;
                }
            }

            // Hide unused blips
            for (int i = blipIndex; i < _blipPool.Count; i++)
            {
                _blipPool[i].gameObject.SetActive(false);
            }

            // Update Info Text at bottom of radar
            if (nearestDistanceText != null)
            {
                float restDist = Vector3.Distance(playerPos, _cachedRestaurantPosition);

                if (closest != null)
                {
                    string recipeName = closest.GetRecipe() != null ? closest.GetRecipe().recipeName : "Order";
                    nearestDistanceText.text = $"{recipeName} • {Mathf.RoundToInt(minDist)}m | Rest: {Mathf.RoundToInt(restDist)}m";
                }
                else
                {
                    nearestDistanceText.text = restDist <= 12f
                        ? "Inside Restaurant"
                        : $"Restaurant • {Mathf.RoundToInt(restDist)}m";
                }
            }
        }

        private RectTransform GetOrCreateBlip(int index)
        {
            if (index < _blipPool.Count) return _blipPool[index];

            var blipObj = new GameObject($"Blip_{index}");
            blipObj.transform.SetParent(blipsContainer != null ? blipsContainer : transform, false);

            var rt = blipObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10f, 10f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = blipObj.AddComponent<Image>();
            img.color = normalBlipColor;

            _blipPool.Add(rt);
            _blipImages.Add(img);
            return rt;
        }

        private void HideAllCustomerBlips()
        {
            foreach (var b in _blipPool)
            {
                if (b != null) b.gameObject.SetActive(false);
            }
            if (_restaurantMarker != null) _restaurantMarker.gameObject.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            float targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }
}
