using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to a burning counter. Manages fire visuals, sizzle sound, and extinguishing health.
/// Implements IHasProgress for automatic progress bar binding.
/// Synchronized across all network clients.
/// </summary>
public class FireHazard : MonoBehaviour, IHasProgress {

    // ===== EVENTS =====
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnIgnited;
    public event EventHandler OnExtinguished;
    public event EventHandler OnHitByExtinguisher;

    [Header("Fire Settings")]
    [SerializeField] private float maxFireHealth = 100f;
    [SerializeField] private float extinguishSpeed = 33.34f; // 100 HP / 3 seconds = ~33.34 HP per second

    private float currentHealth;
    private bool isBurning;
    private GameObject fireVFXInstance;
    private BaseCounter attachedCounter;

    private void Awake() {
        currentHealth = maxFireHealth;
        attachedCounter = GetComponent<BaseCounter>();
    }

    private GameObject fireProgressBarObj;
    private UnityEngine.UI.Image fireBarImage;

    public void Ignite() {
        isBurning = true;
        currentHealth = maxFireHealth;

        EnsureFireVisuals();
        EnsureProgressBarVisual();
        OnIgnited?.Invoke(this, EventArgs.Empty);

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = currentHealth / maxFireHealth
        });
    }

    public void ApplyExtinguish(float deltaTime) {
        if (!isBurning) return;

        currentHealth -= extinguishSpeed * deltaTime;
        OnHitByExtinguisher?.Invoke(this, EventArgs.Empty);

        float progress = Mathf.Clamp01(currentHealth / maxFireHealth);
        if (fireBarImage != null) {
            fireBarImage.fillAmount = progress;
            // Interpolate color from fiery red/orange to extinguished cyan/white
            fireBarImage.color = Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.3f, 0.05f), progress);
        }

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = progress
        });

        if (currentHealth <= 0f) {
            // Request server to extinguish this fire across all clients
            if (attachedCounter != null && KitchenCrisisManager.Instance != null) {
                KitchenCrisisManager.Instance.RequestExtinguishCounterServerRpc(attachedCounter.NetworkObject);
            } else {
                ExtinguishImmediately();
            }
        }
    }

    public void ExtinguishImmediately() {
        if (!isBurning) return;
        isBurning = false;
        currentHealth = 0f;

        if (fireVFXInstance != null) {
            Destroy(fireVFXInstance);
        }

        if (fireProgressBarObj != null) {
            Destroy(fireProgressBarObj);
        }

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        OnExtinguished?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureProgressBarVisual() {
        if (fireProgressBarObj != null) return;

        fireProgressBarObj = new GameObject("FireExtinguishProgressBarUI");
        fireProgressBarObj.transform.SetParent(transform, false);
        fireProgressBarObj.transform.localPosition = new Vector3(0, 2.3f, 0);

        Canvas canvas = fireProgressBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        fireProgressBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        RectTransform rect = fireProgressBarObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110, 22);
        rect.localScale = new Vector3(0.009f, 0.009f, 0.009f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(fireProgressBarObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(100, 18);
        bgRect.anchoredPosition = Vector2.zero;

        var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.92f);

        var outline = bgObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Fill Bar
        GameObject fillObj = new GameObject("BarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(94, 12);
        fillRect.anchoredPosition = Vector2.zero;

        fireBarImage = fillObj.AddComponent<UnityEngine.UI.Image>();
        fireBarImage.color = new Color(1f, 0.3f, 0.05f, 0.95f);
        fireBarImage.type = UnityEngine.UI.Image.Type.Filled;
        fireBarImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fireBarImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        fireBarImage.fillAmount = 1f;

        // Label "FIRE"
        GameObject labelObj = new GameObject("FireLabel");
        labelObj.transform.SetParent(bgObj.transform, false);
        RectTransform lRect = labelObj.AddComponent<RectTransform>();
        lRect.sizeDelta = new Vector2(100, 16);
        lRect.anchoredPosition = new Vector3(0, 18, 0);

        var tmp = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "🔥 FIRE 🔥";
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color = new Color(1f, 0.85f, 0.2f);
    }

    private void LateUpdate() {
        if (fireProgressBarObj != null && fireProgressBarObj.activeSelf) {
            if (Camera.main != null) {
                fireProgressBarObj.transform.forward = Camera.main.transform.forward;
            }
        }
    }

    private void EnsureFireVisuals() {
        if (fireVFXInstance != null) return;

        // Try to instantiate fire visual from existing StoveCounterVisual or create stylized flame particles
        GameObject visualPrefab = Resources.Load<GameObject>("StoveCounter_Visual");
        if (visualPrefab != null) {
            Transform stoveSizzle = visualPrefab.transform.Find("StoveBurnParticlesVisual");
            if (stoveSizzle != null) {
                fireVFXInstance = Instantiate(stoveSizzle.gameObject, transform.position + Vector3.up * 1.2f, Quaternion.identity, transform);
                fireVFXInstance.SetActive(true);
                return;
            }
        }

        // Procedural flame particle container
        fireVFXInstance = CreateFallbackFlameVFX();
    }

    private GameObject CreateFallbackFlameVFX() {
        GameObject flameGo = new GameObject("ProceduralFireVFX");
        flameGo.transform.SetParent(transform);
        flameGo.transform.localPosition = new Vector3(0, 1.2f, 0);

        ParticleSystem ps = flameGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = flameGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null) {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (pShader != null) {
                psr.material = new Material(pShader);
            }
        }
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0f), new Color(1f, 0.8f, 0.1f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startLifetime = 0.8f;
        main.startSpeed = 1.8f;
        main.gravityModifier = -0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 35f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        // Light
        Light fireLight = flameGo.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.1f);
        fireLight.range = 3.5f;
        fireLight.intensity = 2.5f;

        return flameGo;
    }

    public bool IsBurning() => isBurning;
}
