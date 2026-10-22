using System;
using UnityEngine;

/// <summary>
/// Stylized Low-Poly Humanoid Rig and procedural animation controller for player chefs.
/// Replaces flat sphere prototypes with a full character rig featuring a chef toque,
/// apron, articulated limbs, walk blend kinematics, and dynamic carrying poses.
/// </summary>
public class PlayerAvatarVisual : MonoBehaviour
{
    public static void ResetStaticData()
    {
        // Reserved for any avatar-wide static event listeners
    }

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerVisual playerVisual;

    [Header("Rig Elements")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform chefHat;
    [SerializeField] private Transform torso;
    [SerializeField] private Transform apron;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;

    [Header("Kinematic Settings")]
    [SerializeField] private float walkStrideFrequency = 10f;
    [SerializeField] private float legSwingAngle = 32f;
    [SerializeField] private float armSwingAngle = 28f;
    [SerializeField] private float bounceHeight = 0.06f;

    private float _walkCycleTime;
    private Vector3 _baseTorsoLocalPos;
    private Quaternion _leftArmDefaultRot;
    private Quaternion _rightArmDefaultRot;
    private Quaternion _leftLegDefaultRot;
    private Quaternion _rightLegDefaultRot;
    private Quaternion _chefHatDefaultRot;

    private Material _jacketMaterial;
    private Material _apronMaterial;
    private Material _hatMaterial;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }

        if (playerVisual == null)
        {
            playerVisual = GetComponent<PlayerVisual>();
        }

        InitializeProceduralRig();
    }

    private void Start()
    {
        if (player != null && KitchenGameMultiplayer.Instance != null)
        {
            try
            {
                PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(player.OwnerClientId);
                Color teamColor = KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId);
                ApplyCustomizationColors(teamColor);
            }
            catch
            {
                ApplyCustomizationColors(new Color(0.2f, 0.6f, 1f));
            }
        }
    }

    private void InitializeProceduralRig()
    {
        // If limbs are not pre-assigned in prefab, build stylized low-poly chef geometry procedurally
        if (headBone == null)
        {
            GameObject headObj = new GameObject("ChefHead");
            headObj.transform.SetParent(transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            headBone = headObj.transform;
        }

        if (chefHat == null)
        {
            GameObject hatObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatObj.name = "ChefToque";
            hatObj.transform.SetParent(headBone, false);
            hatObj.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            hatObj.transform.localScale = new Vector3(0.48f, 0.32f, 0.48f);
            if (hatObj.TryGetComponent(out Collider c)) Destroy(c);
            chefHat = hatObj.transform;

            _hatMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.96f, 0.96f, 0.98f)
            };
            hatObj.GetComponent<MeshRenderer>().material = _hatMaterial;
        }

        if (torso == null)
        {
            GameObject torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoObj.name = "ChefTorso";
            torsoObj.transform.SetParent(transform, false);
            torsoObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            torsoObj.transform.localScale = new Vector3(0.55f, 0.65f, 0.38f);
            if (torsoObj.TryGetComponent(out Collider c)) Destroy(c);
            torso = torsoObj.transform;

            _jacketMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.9f, 0.9f, 0.9f)
            };
            torsoObj.GetComponent<MeshRenderer>().material = _jacketMaterial;
        }

        if (apron == null && torso != null)
        {
            GameObject apronObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apronObj.name = "ChefApron";
            apronObj.transform.SetParent(torso, false);
            apronObj.transform.localPosition = new Vector3(0f, -0.05f, 0.20f);
            apronObj.transform.localScale = new Vector3(0.56f, 0.70f, 0.06f);
            if (apronObj.TryGetComponent(out Collider c)) Destroy(c);
            apron = apronObj.transform;

            _apronMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.18f, 0.22f, 0.28f) // Dark navy/slate professional chef apron
            };
            apronObj.GetComponent<MeshRenderer>().material = _apronMaterial;
        }

        if (leftArm == null)
        {
            leftArm = CreateLimb("LeftArm", transform, new Vector3(-0.38f, 1.05f, 0f), new Vector3(0.16f, 0.48f, 0.16f), _jacketMaterial);
        }

        if (rightArm == null)
        {
            rightArm = CreateLimb("RightArm", transform, new Vector3(0.38f, 1.05f, 0f), new Vector3(0.16f, 0.48f, 0.16f), _jacketMaterial);
        }

        if (leftLeg == null)
        {
            leftLeg = CreateLimb("LeftLeg", transform, new Vector3(-0.16f, 0.45f, 0f), new Vector3(0.18f, 0.50f, 0.18f), _apronMaterial);
        }

        if (rightLeg == null)
        {
            rightLeg = CreateLimb("RightLeg", transform, new Vector3(0.16f, 0.45f, 0f), new Vector3(0.18f, 0.50f, 0.18f), _apronMaterial);
        }

        if (torso != null) _baseTorsoLocalPos = torso.localPosition;
        if (leftArm != null) _leftArmDefaultRot = leftArm.localRotation;
        if (rightArm != null) _rightArmDefaultRot = rightArm.localRotation;
        if (leftLeg != null) _leftLegDefaultRot = leftLeg.localRotation;
        if (rightLeg != null) _rightLegDefaultRot = rightLeg.localRotation;
        if (chefHat != null) _chefHatDefaultRot = chefHat.localRotation;
    }

    private Transform CreateLimb(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject pivot = new GameObject(name + "_Pivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = name + "_Mesh";
        visual.transform.SetParent(pivot.transform, false);
        visual.transform.localPosition = new Vector3(0f, -scale.y * 0.5f, 0f);
        visual.transform.localScale = scale;
        if (visual.TryGetComponent(out Collider c)) Destroy(c);
        if (mat != null) visual.GetComponent<MeshRenderer>().material = mat;

        return pivot.transform;
    }

    public void ApplyCustomizationColors(Color teamColor)
    {
        if (_jacketMaterial != null)
        {
            _jacketMaterial.color = teamColor;
        }

        if (playerVisual != null)
        {
            playerVisual.SetPlayerColor(teamColor);
        }
    }

    private void Update()
    {
        bool isWalking = player != null && player.IsWalking();
        bool isMounted = player != null && player.IsMountedOnVehicle();
        bool isCarrying = player != null && player.HasKitchenObject();

        if (isWalking && !isMounted)
        {
            _walkCycleTime += Time.deltaTime * walkStrideFrequency;
            float legSin = Mathf.Sin(_walkCycleTime);
            float armSin = Mathf.Sin(_walkCycleTime);

            // Alternate leg swing
            if (leftLeg != null) leftLeg.localRotation = _leftLegDefaultRot * Quaternion.Euler(legSin * legSwingAngle, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = _rightLegDefaultRot * Quaternion.Euler(-legSin * legSwingAngle, 0f, 0f);

            // Alternate arm swing (or lock into carry pose if holding food)
            if (isCarrying)
            {
                ApplyCarryingPose();
            }
            else
            {
                if (leftArm != null) leftArm.localRotation = _leftArmDefaultRot * Quaternion.Euler(-armSin * armSwingAngle, 0f, 0f);
                if (rightArm != null) rightArm.localRotation = _rightArmDefaultRot * Quaternion.Euler(armSin * armSwingAngle, 0f, 0f);
            }

            // Torso bounce
            if (torso != null)
            {
                float bounce = Mathf.Abs(Mathf.Sin(_walkCycleTime)) * bounceHeight;
                torso.localPosition = _baseTorsoLocalPos + new Vector3(0f, bounce, 0f);
            }

            // Chef hat playful secondary sway
            if (chefHat != null)
            {
                float sway = Mathf.Cos(_walkCycleTime * 0.5f) * 4f;
                chefHat.localRotation = _chefHatDefaultRot * Quaternion.Euler(sway, 0f, sway * 0.5f);
            }
        }
        else if (isMounted)
        {
            // Scooter riding pose
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(45f, 0f, -10f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(45f, 0f, 10f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-55f, 20f, 0f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-55f, -20f, 0f);
            if (torso != null) torso.localPosition = _baseTorsoLocalPos + new Vector3(0f, -0.1f, -0.05f);
        }
        else
        {
            // Idle or carrying idle
            if (isCarrying)
            {
                ApplyCarryingPose();
            }
            else
            {
                ResetPose();
            }
        }
    }

    private void ApplyCarryingPose()
    {
        // Smoothly angle arms forward to hold plate or bag steady
        Quaternion carryRot = Quaternion.Euler(-50f, 0f, 0f);
        if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, carryRot, Time.deltaTime * 12f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, carryRot, Time.deltaTime * 12f);
    }

    private void ResetPose()
    {
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, _leftLegDefaultRot, Time.deltaTime * 8f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, _rightLegDefaultRot, Time.deltaTime * 8f);
        if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, _leftArmDefaultRot, Time.deltaTime * 8f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, _rightArmDefaultRot, Time.deltaTime * 8f);
        if (torso != null) torso.localPosition = Vector3.Lerp(torso.localPosition, _baseTorsoLocalPos, Time.deltaTime * 8f);
        if (chefHat != null) chefHat.localRotation = Quaternion.Slerp(chefHat.localRotation, _chefHatDefaultRot, Time.deltaTime * 8f);
    }
}
