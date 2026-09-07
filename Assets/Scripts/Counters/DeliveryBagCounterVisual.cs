using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the visual representation of the DeliveryBagCounter, making it distinctly recognizable
/// from regular kitchen counters with custom courier styling, top bag preview models, and dispenser animations.
/// </summary>
public class DeliveryBagCounterVisual : MonoBehaviour
{
    [SerializeField] private DeliveryBagCounter deliveryBagCounter;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private Transform bagVisualPrefab;

    private GameObject bagDisplayVisual;
    private Coroutine bounceCoroutine;

    private void Awake()
    {
        if (deliveryBagCounter == null)
        {
            deliveryBagCounter = GetComponentInParent<DeliveryBagCounter>();
        }
    }

    private void Start()
    {
        if (deliveryBagCounter != null)
        {
            deliveryBagCounter.OnBagDispensed += DeliveryBagCounter_OnBagDispensed;
        }

        SetupDistinctCounterVisuals();
        CreateBagDisplayVisual();
    }

    private void Update()
    {
        // Hide the dummy bag visual if an actual KitchenObject is placed on the counter
        if (bagDisplayVisual != null && deliveryBagCounter != null)
        {
            bool hasRealObject = deliveryBagCounter.HasKitchenObject();
            if (bagDisplayVisual.activeSelf == hasRealObject)
            {
                bagDisplayVisual.SetActive(!hasRealObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (deliveryBagCounter != null)
        {
            deliveryBagCounter.OnBagDispensed -= DeliveryBagCounter_OnBagDispensed;
        }
    }

    private void DeliveryBagCounter_OnBagDispensed(object sender, EventArgs e)
    {
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        bounceCoroutine = StartCoroutine(AnimateDispenseBounce());
    }

    private IEnumerator AnimateDispenseBounce()
    {
        if (bagDisplayVisual == null) yield break;

        Vector3 originalScale = bagDisplayVisual.transform.localScale;
        Vector3 punchScale = originalScale * 1.25f;

        float duration = 0.15f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            bagDisplayVisual.transform.localScale = Vector3.Lerp(originalScale, punchScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        bagDisplayVisual.transform.localScale = originalScale;
    }

    private void CreateBagDisplayVisual()
    {
        Transform parentPoint = counterTopPoint != null ? counterTopPoint : transform;

        // If a bag prefab is assigned, instantiate a non-colliding visual clone
        if (bagVisualPrefab != null)
        {
            Transform spawned = Instantiate(bagVisualPrefab, parentPoint);
            spawned.localPosition = Vector3.zero;
            spawned.localRotation = Quaternion.identity;

            // Remove any network / kitchen logic / colliders from display dummy
            Destroy(spawned.GetComponent<Collider>());
            var netObj = spawned.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null) Destroy(netObj);
            var bagLogic = spawned.GetComponent<DeliveryBagKitchenObject>();
            if (bagLogic != null) Destroy(bagLogic);

            bagDisplayVisual = spawned.gameObject;
        }
        else
        {
            // Build a visual courier bag stack dummy on the counter
            GameObject dummy = new GameObject("DeliveryBag_DisplayVisual");
            dummy.transform.SetParent(parentPoint, false);
            dummy.transform.localPosition = new Vector3(0, 0.05f, 0);

            // Bag Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BagBody_Dummy";
            body.transform.SetParent(dummy.transform, false);
            body.transform.localPosition = new Vector3(0, 0.18f, 0);
            body.transform.localScale = new Vector3(0.44f, 0.36f, 0.34f);
            Destroy(body.GetComponent<Collider>());

            // Color body warm delivery orange / amber
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.95f, 0.45f, 0.1f); // Vibrant delivery orange
                renderer.material = mat;
            }

            // Bag Lid
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "BagLid_Dummy";
            lid.transform.SetParent(dummy.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.38f, 0);
            lid.transform.localScale = new Vector3(0.46f, 0.06f, 0.36f);
            Destroy(lid.GetComponent<Collider>());

            var lidRenderer = lid.GetComponent<MeshRenderer>();
            if (lidRenderer != null)
            {
                Material lidMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                lidMat.color = new Color(0.2f, 0.2f, 0.22f); // Dark strap/lid
                lidRenderer.material = lidMat;
            }

            bagDisplayVisual = dummy;
        }
    }

    private void SetupDistinctCounterVisuals()
    {
        // Add a stylized front sign / badge on the counter facing forward
        GameObject signObj = new GameObject("DeliveryBag_FrontSign");
        signObj.transform.SetParent(transform, false);
        signObj.transform.localPosition = new Vector3(0f, 0.75f, -0.76f);
        signObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        Canvas canvas = signObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = signObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.2f, 0.4f);
        signObj.transform.localScale = Vector3.one * 0.01f;

        // Background Plate
        GameObject bgObj = new GameObject("SignBG");
        bgObj.transform.SetParent(signObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Orange Accent Trim
        GameObject trimObj = new GameObject("AccentTrim");
        trimObj.transform.SetParent(signObj.transform, false);
        Image trimImage = trimObj.AddComponent<Image>();
        trimImage.color = new Color(0.95f, 0.45f, 0.1f, 1f);
        RectTransform trimRt = trimObj.GetComponent<RectTransform>();
        trimRt.anchorMin = new Vector2(0, 0);
        trimRt.anchorMax = new Vector2(1, 0.12f);
        trimRt.sizeDelta = Vector2.zero;

        // Label Text
        GameObject textObj = new GameObject("SignText");
        textObj.transform.SetParent(signObj.transform, false);
        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = "DELIVERY BAGS";
        label.fontSize = 14;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.9f, 0.7f);
        label.alignment = TextAlignmentOptions.Center;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
    }
}
