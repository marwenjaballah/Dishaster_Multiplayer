using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A water leak / burst pipe hazard that sprays water particles and creates slippery puddles.
/// Players must walk up and interact (E key) with the Pipe Wrench 3 times to tighten the valve and fix the leak.
/// </summary>
public class WaterLeakHazard : NetworkBehaviour, IHasProgress {

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnLeakFixed;

    [Header("Repair Settings")]
    [SerializeField] private int clicksRequired = 3;
    private NetworkVariable<int> repairClicks = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isLeaking = new NetworkVariable<bool>(false);

    private GameObject sprayObj;
    private GameObject splashObj;
    private ParticleSystem waterSprayParticles;
    private ParticleSystem waterSplashParticles;
    private GameObject puddleObject;
    [SerializeField] private float minPuddleRadius = 0.8f;
    [SerializeField] private float maxPuddleRadius = 4.5f;
    [SerializeField] private float puddleGrowthRate = 0.22f; // Expands radius smoothly over time
    private float currentPuddleRadius = 0.8f;
    private Vector3 puddleLocalOffset = new Vector3(0, 0.02f, 0.7f);

    private void Awake() {
        SetupVisuals();
        StopLeakVisualsInstant();
    }

    private void Start() {
        if (!isLeaking.Value) {
            StopLeakVisualsInstant();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        repairClicks.OnValueChanged += (prev, curr) => {
            float normalized = (float)curr / clicksRequired;
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
                progressNormalized = normalized
            });
        };

        isLeaking.OnValueChanged += (prev, curr) => {
            if (curr) {
                StartLeakVisuals();
            } else {
                StopLeakVisuals();
            }
        };

        if (isLeaking.Value) {
            StartLeakVisuals();
        } else {
            StopLeakVisualsInstant();
        }
    }

    private void SetupVisuals() {
        Material waterMat = Resources.Load<Material>("WaterLeakSpray");
        if (waterMat == null) {
            #if UNITY_EDITOR
            waterMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Assets/Materials/WaterLeakSpray.mat");
            #endif
        }
        if (waterMat == null) {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (urpShader != null) {
                waterMat = new Material(urpShader);
                waterMat.color = new Color(0.35f, 0.75f, 1.0f, 0.8f);
            }
        }

        Material puddleMat = Resources.Load<Material>("WaterPuddle");
        if (puddleMat == null) {
            #if UNITY_EDITOR
            puddleMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Assets/Materials/WaterPuddle.mat");
            #endif
        }
        if (puddleMat == null) {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader != null) {
                puddleMat = new Material(litShader);
                puddleMat.color = new Color(0.2f, 0.6f, 0.95f, 0.6f);
            }
        }

        // 1. Water spray geyser particle system (arcs out and down)
        sprayObj = new GameObject("WaterSprayFX");
        sprayObj.transform.SetParent(transform, false);
        sprayObj.transform.localPosition = new Vector3(0, 1.25f, 0.2f);
        sprayObj.transform.localRotation = Quaternion.Euler(25, 0, 0);

        waterSprayParticles = sprayObj.AddComponent<ParticleSystem>();
        var main = waterSprayParticles.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 4.0f;
        main.startSize = 0.18f;
        main.gravityModifier = 1.6f; // Water arcs down naturally to the floor!
        main.startColor = new Color(0.45f, 0.8f, 1.0f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;

        var emission = waterSprayParticles.emission;
        emission.rateOverTime = 80f;

        var shape = waterSprayParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.08f;

        var col = waterSprayParticles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.2f, 0.5f, 0.9f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.1f, 1f) }
        );
        col.color = grad;

        var sizeOverLifetime = waterSprayParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(1f, 1.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var pRenderer = waterSprayParticles.GetComponent<ParticleSystemRenderer>();
        if (waterMat != null) pRenderer.material = waterMat;
        waterSprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        sprayObj.SetActive(false);

        // 2. Ground splash mist & droplets
        splashObj = new GameObject("WaterSplashFX");
        splashObj.transform.SetParent(transform, false);
        splashObj.transform.localPosition = new Vector3(0, 0.05f, 0.6f);
        splashObj.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        waterSplashParticles = splashObj.AddComponent<ParticleSystem>();
        var splashMain = waterSplashParticles.main;
        splashMain.startLifetime = 0.4f;
        splashMain.startSpeed = 2.5f;
        splashMain.startSize = 0.15f;
        splashMain.gravityModifier = 0.5f;
        splashMain.startColor = new Color(0.6f, 0.85f, 1.0f, 0.7f);
        splashMain.simulationSpace = ParticleSystemSimulationSpace.World;
        splashMain.loop = true;
        splashMain.playOnAwake = false;

        var splashEmission = waterSplashParticles.emission;
        splashEmission.rateOverTime = 40f;

        var splashShape = waterSplashParticles.shape;
        splashShape.shapeType = ParticleSystemShapeType.Circle;
        splashShape.radius = 0.5f;

        var splashRenderer = waterSplashParticles.GetComponent<ParticleSystemRenderer>();
        if (waterMat != null) splashRenderer.material = waterMat;
        waterSplashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        splashObj.SetActive(false);

        // 3. Slippery floor puddle
        puddleObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        puddleObject.name = "WaterPuddle";
        puddleObject.transform.SetParent(transform, false);
        puddleObject.transform.localPosition = puddleLocalOffset;
        puddleObject.transform.localScale = new Vector3(currentPuddleRadius * 2f, 0.005f, currentPuddleRadius * 2f);

        Collider c = puddleObject.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        if (puddleMat != null) {
            puddleObject.GetComponent<Renderer>().material = puddleMat;
        }

        puddleObject.SetActive(false);
    }

    private void Update() {
        if (!isLeaking.Value) return;

        // Puddle expands progressively over time up to maxPuddleRadius
        if (currentPuddleRadius < maxPuddleRadius) {
            currentPuddleRadius = Mathf.Min(currentPuddleRadius + Time.deltaTime * puddleGrowthRate, maxPuddleRadius);
            
            if (puddleObject != null) {
                puddleObject.transform.localScale = new Vector3(currentPuddleRadius * 2f, 0.005f, currentPuddleRadius * 2f);
            }

            // Dynamically scale splash particle radius & intensity with the expanding puddle
            if (waterSplashParticles != null) {
                var splashShape = waterSplashParticles.shape;
                splashShape.radius = Mathf.Clamp(currentPuddleRadius * 0.65f, 0.5f, 3.0f);

                var splashEmission = waterSplashParticles.emission;
                float growthRatio = Mathf.Clamp01((currentPuddleRadius - minPuddleRadius) / (maxPuddleRadius - minPuddleRadius));
                splashEmission.rateOverTime = Mathf.Lerp(40f, 85f, growthRatio);
            }
        }

        // Apply slowdown to local player if standing anywhere inside the expanding puddle range
        if (Player.LocalInstance != null) {
            Vector3 puddleWorldPos = transform.TransformPoint(puddleLocalOffset);
            puddleWorldPos.y = Player.LocalInstance.transform.position.y;
            float distToPuddle = Vector3.Distance(Player.LocalInstance.transform.position, puddleWorldPos);

            if (distToPuddle <= currentPuddleRadius) {
                Player.LocalInstance.SetSpeedMultiplier(0.55f);
            }
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!isLeaking.Value) return;

        // Fallback for trigger colliders
        Player player = other.GetComponent<Player>();
        if (player != null) {
            player.SetSpeedMultiplier(0.55f);
        }
    }

    public void StartLeak() {
        if (!IsServer) return;
        repairClicks.Value = 0;
        isLeaking.Value = true;
    }

    public void InteractRepair() {
        if (!isLeaking.Value) return;
        InteractRepairServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractRepairServerRpc() {
        if (!isLeaking.Value) return;

        repairClicks.Value++;
        if (repairClicks.Value >= clicksRequired) {
            isLeaking.Value = false;
            repairClicks.Value = 0;
            OnLeakFixed?.Invoke(this, EventArgs.Empty);

            if (KitchenCrisisManager.Instance != null) {
                KitchenCrisisManager.Instance.EndWaterLeakServerRpc();
            }
        }
    }

    public void StopLeakImmediately() {
        if (IsServer) {
            isLeaking.Value = false;
            repairClicks.Value = 0;
        }
        StopLeakVisuals();
    }

    private void StartLeakVisuals() {
        currentPuddleRadius = minPuddleRadius;
        if (sprayObj != null) sprayObj.SetActive(true);
        if (splashObj != null) splashObj.SetActive(true);

        if (waterSprayParticles != null) {
            waterSprayParticles.Clear();
            waterSprayParticles.Play();
        }
        if (waterSplashParticles != null) {
            var splashShape = waterSplashParticles.shape;
            splashShape.radius = minPuddleRadius * 0.65f;
            waterSplashParticles.Clear();
            waterSplashParticles.Play();
        }
        if (puddleObject != null) {
            puddleObject.SetActive(true);
            puddleObject.transform.localScale = new Vector3(currentPuddleRadius * 2f, 0.005f, currentPuddleRadius * 2f);
        }
    }

    private void StopLeakVisuals() {
        if (waterSprayParticles != null) waterSprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (waterSplashParticles != null) waterSplashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (puddleObject != null) {
            StartCoroutine(EvaporatePuddleRoutine());
        }
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });
    }

    private void StopLeakVisualsInstant() {
        if (waterSprayParticles != null) waterSprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (waterSplashParticles != null) waterSplashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sprayObj != null) sprayObj.SetActive(false);
        if (splashObj != null) splashObj.SetActive(false);
        if (puddleObject != null) puddleObject.SetActive(false);
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });
    }

    private IEnumerator EvaporatePuddleRoutine() {
        float elapsed = 0f;
        float duration = 1.5f;
        Vector3 initialScale = puddleObject != null ? puddleObject.transform.localScale : Vector3.zero;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (puddleObject != null) {
                puddleObject.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            }
            yield return null;
        }

        if (puddleObject != null) {
            puddleObject.SetActive(false);
        }
        if (sprayObj != null) sprayObj.SetActive(false);
        if (splashObj != null) splashObj.SetActive(false);
    }

    public bool IsLeaking() => isLeaking.Value;
}
