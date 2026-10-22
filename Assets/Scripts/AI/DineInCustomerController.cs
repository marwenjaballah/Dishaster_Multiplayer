using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 3D Animated Dine-In Customer NPC.
/// Walks into the restaurant from the entrance, navigates to the assigned table,
/// sits down in the booth, orders food, eats upon delivery, leaves dirty plates,
/// and walks back out to the entrance.
/// Features full kinematic procedural locomotion and sitting/eating gestures.
/// </summary>
public class DineInCustomerController : MonoBehaviour {

    public static event EventHandler OnAnyCustomerSeated;
    public static event EventHandler OnAnyCustomerLeft;

    public static void ResetStaticData() {
        OnAnyCustomerSeated = null;
        OnAnyCustomerLeft = null;
    }

    public enum CustomerState {
        WalkingToTable,
        Sitting,
        Eating,
        Leaving,
        Despawned
    }

    [Header("State")]
    [SerializeField] private CustomerState currentState = CustomerState.WalkingToTable;

    [Header("Gait Settings")]
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private float strideLength = 0.75f;
    [SerializeField] private float maxLegAngle = 28f;
    [SerializeField] private float maxArmAngle = 22f;

    [Header("Limb Pivots")]
    [SerializeField] private Transform hipL;
    [SerializeField] private Transform hipR;
    [SerializeField] private Transform shoulderL;
    [SerializeField] private Transform shoulderR;
    [SerializeField] private Transform torso;
    [SerializeField] private Transform head;

    private CustomerTable targetTable;
    private Vector3 seatPosition;
    private Quaternion seatRotation;
    private Vector3 entrancePosition;
    private float gaitPhase;
    private float walkWeight = 1f;
    private Vector3 initialTorsoPos;
    private Coroutine eatingRoutine;

    public void Initialize(CustomerTable table, Vector3 entrancePos) {
        EnsureLimbPivots();
        targetTable = table;
        entrancePosition = entrancePos;

        Transform stool = ResolveStool(table);
        if (stool != null) {
            seatPosition = stool.position;
            Vector3 toTable = table.transform.position - stool.position;
            toTable.y = 0f;
            seatRotation = toTable != Vector3.zero ? Quaternion.LookRotation(toTable) : Quaternion.identity;
        } else {
            Vector3 tablePos = table.transform.position;
            seatPosition = tablePos + new Vector3(0f, 0f, 1.15f);
            seatRotation = Quaternion.LookRotation(table.transform.position - seatPosition);
        }

        transform.position = entrancePosition;
        currentState = CustomerState.WalkingToTable;
        walkWeight = 1f;

        // Subscribe to table events
        CustomerTable.OnAnyTableDeliverySuccess += CustomerTable_OnAnyTableDeliverySuccess;
        CustomerTable.OnAnyTableDeliveryFailed += CustomerTable_OnAnyTableDeliveryFailed;
    }

    private Transform ResolveStool(CustomerTable table) {
        if (table == null) return null;
        int num = table.GetDisplayNumber();

        // 1. Look for designated EggStools by display number
        GameObject stoolL = GameObject.Find($"EggStool_{num}_Left");
        if (stoolL != null) return stoolL.transform;

        GameObject stoolR = GameObject.Find($"EggStool_{num}_Right");
        if (stoolR != null) return stoolR.transform;

        // 2. Overlap search within 2.5m for EggStool
        Collider[] nearby = Physics.OverlapSphere(table.transform.position, 2.5f);
        foreach (var col in nearby) {
            if (col.gameObject.name.Contains("Stool")) return col.transform;
        }

        return null;
    }

    private void EnsureLimbPivots() {
        if (torso == null) torso = transform.Find("Torso");
        if (torso == null) {
            BuildProceduralAvatar();
        }

        if (head == null && torso != null) head = torso.Find("Head");
        if (hipL == null) hipL = transform.Find("Hip_L");
        if (hipR == null) hipR = transform.Find("Hip_R");
        if (shoulderL == null) shoulderL = transform.Find("Shoulder_L");
        if (shoulderR == null) shoulderR = transform.Find("Shoulder_R");

        if (torso != null) initialTorsoPos = torso.localPosition;
    }

    private void BuildProceduralAvatar() {
        Color[] shirtColors = new Color[] {
            new Color(0.2f, 0.6f, 0.85f), // Sky Blue
            new Color(0.85f, 0.35f, 0.25f), // Coral
            new Color(0.3f, 0.75f, 0.45f), // Emerald
            new Color(0.75f, 0.5f, 0.85f), // Lavender
            new Color(0.95f, 0.7f, 0.2f)   // Mustard
        };
        Color shirtColor = shirtColors[UnityEngine.Random.Range(0, shirtColors.Length)];
        Color pantsColor = new Color(0.18f, 0.22f, 0.28f); // Dark Denim
        Color skinColor = new Color(0.92f, 0.76f, 0.64f);  // Warm Skin
        Color hairColor = new Color(0.25f, 0.15f, 0.1f);   // Espresso

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        Material shirtMat = new Material(urpShader) { color = shirtColor };
        Material pantsMat = new Material(urpShader) { color = pantsColor };
        Material skinMat = new Material(urpShader) { color = skinColor };
        Material hairMat = new Material(urpShader) { color = hairColor };

        // Torso
        GameObject torsoGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torsoGo.name = "Torso";
        torsoGo.transform.SetParent(transform, false);
        torsoGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        torsoGo.transform.localScale = new Vector3(0.38f, 0.42f, 0.26f);
        torsoGo.GetComponent<MeshRenderer>().sharedMaterial = shirtMat;
        StripColliders(torsoGo);
        torso = torsoGo.transform;

        // Head
        GameObject headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGo.name = "Head";
        headGo.transform.SetParent(torso, false);
        headGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        headGo.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        headGo.GetComponent<MeshRenderer>().sharedMaterial = skinMat;
        StripColliders(headGo);
        head = headGo.transform;

        // Hair
        GameObject hairGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hairGo.name = "Hair";
        hairGo.transform.SetParent(head, false);
        hairGo.transform.localPosition = new Vector3(0f, 0.2f, -0.06f);
        hairGo.transform.localScale = new Vector3(1.06f, 0.7f, 1.06f);
        hairGo.GetComponent<MeshRenderer>().sharedMaterial = hairMat;
        StripColliders(hairGo);

        // Hips & Legs
        hipL = CreateLimbPivot("Hip_L", transform, new Vector3(-0.14f, 0.72f, 0f));
        CreateLimbMesh("Leg_L", hipL, new Vector3(0f, -0.32f, 0f), new Vector3(0.14f, 0.36f, 0.14f), pantsMat);

        hipR = CreateLimbPivot("Hip_R", transform, new Vector3(0.14f, 0.72f, 0f));
        CreateLimbMesh("Leg_R", hipR, new Vector3(0f, -0.32f, 0f), new Vector3(0.14f, 0.36f, 0.14f), pantsMat);

        // Shoulders & Arms
        shoulderL = CreateLimbPivot("Shoulder_L", transform, new Vector3(-0.24f, 1.18f, 0f));
        CreateLimbMesh("Arm_L", shoulderL, new Vector3(0f, -0.28f, 0f), new Vector3(0.11f, 0.30f, 0.11f), shirtMat);

        shoulderR = CreateLimbPivot("Shoulder_R", transform, new Vector3(0.24f, 1.18f, 0f));
        CreateLimbMesh("Arm_R", shoulderR, new Vector3(0f, -0.28f, 0f), new Vector3(0.11f, 0.30f, 0.11f), shirtMat);
    }

    private Transform CreateLimbPivot(string name, Transform parent, Vector3 localPos) {
        GameObject pivotGo = new GameObject(name);
        pivotGo.transform.SetParent(parent, false);
        pivotGo.transform.localPosition = localPos;
        return pivotGo.transform;
    }

    private void CreateLimbMesh(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat) {
        GameObject meshGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        meshGo.name = name;
        meshGo.transform.SetParent(parent, false);
        meshGo.transform.localPosition = localPos;
        meshGo.transform.localScale = localScale;
        meshGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        StripColliders(meshGo);
    }

    private void StripColliders(GameObject go) {
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    private void Update() {
        switch (currentState) {
            case CustomerState.WalkingToTable:
                MoveTowards(seatPosition, () => {
                    // Arrived at table chair
                    currentState = CustomerState.Sitting;
                    transform.position = seatPosition;
                    transform.rotation = seatRotation;
                    ApplySittingPose();

                    // Notify table so the order panel and timer top show up only after customer is seated
                    if (targetTable != null) {
                        targetTable.SetCustomerSeated(true);
                    }
                    OnAnyCustomerSeated?.Invoke(this, EventArgs.Empty);
                });
                break;

            case CustomerState.Sitting:
                AnimateIdleSitting();
                break;

            case CustomerState.Eating:
                AnimateEating();
                break;

            case CustomerState.Leaving:
                MoveTowards(entrancePosition, () => {
                    currentState = CustomerState.Despawned;
                    OnAnyCustomerLeft?.Invoke(this, EventArgs.Empty);
                    Destroy(gameObject);
                });
                break;
        }
    }

    private void MoveTowards(Vector3 target, Action onArrived) {
        Vector3 currentPos = transform.position;
        Vector3 dir = (target - currentPos);
        dir.y = 0f;

        float dist = dir.magnitude;
        if (dist <= 0.35f) {
            onArrived?.Invoke();
            return;
        }

        Vector3 moveDir = dir.normalized;
        float moveDist = walkSpeed * Time.deltaTime;
        transform.position += moveDir * moveDist;

        if (moveDir != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
        }

        // Animate procedural walk
        gaitPhase += (moveDist / strideLength) * Mathf.PI * 2f;
        walkWeight = Mathf.MoveTowards(walkWeight, 1f, Time.deltaTime * 5f);

        float legPitch = Mathf.Sin(gaitPhase) * maxLegAngle * walkWeight;
        if (hipL != null) hipL.localRotation = Quaternion.Euler(legPitch, 0f, 0f);
        if (hipR != null) hipR.localRotation = Quaternion.Euler(-legPitch, 0f, 0f);

        float armPitch = -Mathf.Sin(gaitPhase) * maxArmAngle * walkWeight;
        if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(armPitch, 0f, -4f);
        if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(-armPitch, 0f, 4f);

        if (torso != null) {
            torso.localPosition = initialTorsoPos + new Vector3(0f, Mathf.Abs(Mathf.Sin(gaitPhase)) * 0.04f, 0f);
        }
    }

    private void ApplySittingPose() {
        // Bend hips 85 degrees forward over chair seat
        if (hipL != null) hipL.localRotation = Quaternion.Euler(85f, -6f, 0f);
        if (hipR != null) hipR.localRotation = Quaternion.Euler(85f, 6f, 0f);

        // Rest arms forward toward table
        if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(-42f, 18f, 0f);
        if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(-42f, -18f, 0f);

        // Lower torso to sit squarely on chair cushion
        if (torso != null) {
            torso.localPosition = initialTorsoPos + new Vector3(0f, -0.22f, 0f);
        }
    }

    private void AnimateIdleSitting() {
        // Gentle breathing and looking around
        float breathe = Mathf.Sin(Time.time * 2.0f) * 1.5f;
        if (torso != null) {
            torso.localRotation = Quaternion.Euler(breathe, 0f, 0f);
        }
        if (head != null) {
            float lookYaw = Mathf.Sin(Time.time * 0.8f) * 12f;
            head.localRotation = Quaternion.Euler(0f, lookYaw, 0f);
        }
    }

    private void AnimateEating() {
        // Enthusiastic eating nods
        float eatBob = Mathf.Sin(Time.time * 6.0f) * 8f;
        if (head != null) {
            head.localRotation = Quaternion.Euler(eatBob + 15f, 0f, 0f);
        }
        if (shoulderL != null) {
            shoulderL.localRotation = Quaternion.Euler(55f + eatBob * 0.5f, 0f, -8f);
        }
        if (shoulderR != null) {
            shoulderR.localRotation = Quaternion.Euler(55f - eatBob * 0.5f, 0f, 8f);
        }
    }

    private void CustomerTable_OnAnyTableDeliverySuccess(object sender, EventArgs e) {
        if (sender is CustomerTable table && table == targetTable && currentState == CustomerState.Sitting) {
            if (eatingRoutine != null) StopCoroutine(eatingRoutine);
            eatingRoutine = StartCoroutine(EatAndLeaveRoutine());
        }
    }

    private void CustomerTable_OnAnyTableDeliveryFailed(object sender, EventArgs e) {
        if (sender is CustomerTable table && table == targetTable) {
            if (currentState == CustomerState.Sitting || currentState == CustomerState.WalkingToTable) {
                if (eatingRoutine != null) StopCoroutine(eatingRoutine);
                StartLeaving();
            }
        }
    }

    private IEnumerator EatAndLeaveRoutine() {
        currentState = CustomerState.Eating;
        yield return new WaitForSeconds(3.5f);
        StartLeaving();
    }

    private void StartLeaving() {
        currentState = CustomerState.Leaving;
        walkWeight = 1f;
        targetTable?.SetCustomerSeated(false);

        // Reset sitting pose back to standing
        if (hipL != null) hipL.localRotation = Quaternion.identity;
        if (hipR != null) hipR.localRotation = Quaternion.identity;
        if (shoulderL != null) shoulderL.localRotation = Quaternion.identity;
        if (shoulderR != null) shoulderR.localRotation = Quaternion.identity;
        if (torso != null) {
            torso.localPosition = initialTorsoPos;
            torso.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy() {
        CustomerTable.OnAnyTableDeliverySuccess -= CustomerTable_OnAnyTableDeliverySuccess;
        CustomerTable.OnAnyTableDeliveryFailed -= CustomerTable_OnAnyTableDeliveryFailed;
    }
}
