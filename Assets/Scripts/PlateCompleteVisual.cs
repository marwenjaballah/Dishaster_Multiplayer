using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateCompleteVisual : MonoBehaviour {


    [Serializable]
    public struct KitchenObjectSO_GameObject {

        public KitchenObjectSO kitchenObjectSO;
        public GameObject gameObject;

    }


    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> kitchenObjectSOGameObjectList;


    private void Start() {
        plateKitchenObject.OnIngredientAdded += PlateKitchenObject_OnIngredientAdded;

        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            kitchenObjectSOGameObject.gameObject.SetActive(false);
        }
    }

    private GameObject _proceduralFriesVisual;
    private GameObject _proceduralSodaVisual;

    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.OnIngredientAddedEventArgs e) {
        bool found = false;
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            if (kitchenObjectSOGameObject.kitchenObjectSO == e.kitchenObjectSO) {
                kitchenObjectSOGameObject.gameObject.SetActive(true);
                found = true;
            }
        }

        if (!found && e.kitchenObjectSO != null) {
            string objName = e.kitchenObjectSO.name;
            if (objName.Contains("Fries") && _proceduralFriesVisual == null) {
                _proceduralFriesVisual = CreateFriesSideVisual();
            } else if (objName.Contains("Soda") && _proceduralSodaVisual == null) {
                _proceduralSodaVisual = CreateSodaSideVisual();
            }
        }
    }

    private GameObject CreateFriesSideVisual() {
        GameObject friesContainer = new GameObject("Side_FrenchFries");
        friesContainer.transform.SetParent(transform, false);
        friesContainer.transform.localPosition = new Vector3(-0.24f, 0.08f, 0.16f);
        friesContainer.transform.localRotation = Quaternion.Euler(10f, 25f, 0f);
        friesContainer.transform.localScale = Vector3.one * 0.75f;

        // Fry packet
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "FryBox";
        box.transform.SetParent(friesContainer.transform, false);
        box.transform.localScale = new Vector3(0.18f, 0.22f, 0.10f);
        Destroy(box.GetComponent<Collider>());
        var boxRenderer = box.GetComponent<MeshRenderer>();
        if (boxRenderer != null) {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.9f, 0.18f, 0.18f); // Red fry box
            boxRenderer.material = mat;
        }

        // Golden Fries
        for (int i = 0; i < 4; i++) {
            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stick.name = $"FryStick_{i}";
            stick.transform.SetParent(friesContainer.transform, false);
            stick.transform.localPosition = new Vector3(-0.04f + (i * 0.028f), 0.12f, 0f);
            stick.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-12f, 12f));
            stick.transform.localScale = new Vector3(0.022f, 0.14f, 0.022f);
            Destroy(stick.GetComponent<Collider>());
            var stickRenderer = stick.GetComponent<MeshRenderer>();
            if (stickRenderer != null) {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.95f, 0.76f, 0.12f); // Golden fry yellow
                stickRenderer.material = mat;
            }
        }

        return friesContainer;
    }

    private GameObject CreateSodaSideVisual() {
        GameObject sodaContainer = new GameObject("Side_FountainSoda");
        sodaContainer.transform.SetParent(transform, false);
        sodaContainer.transform.localPosition = new Vector3(0.24f, 0.09f, 0.16f);
        sodaContainer.transform.localScale = Vector3.one * 0.75f;

        // Cup
        GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "SodaCup";
        cup.transform.SetParent(sodaContainer.transform, false);
        cup.transform.localScale = new Vector3(0.14f, 0.13f, 0.14f);
        Destroy(cup.GetComponent<Collider>());
        var cupRenderer = cup.GetComponent<MeshRenderer>();
        if (cupRenderer != null) {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.15f, 0.45f, 0.95f); // Refreshing blue soda cup
            cupRenderer.material = mat;
        }

        // Straw
        GameObject straw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        straw.name = "Straw";
        straw.transform.SetParent(sodaContainer.transform, false);
        straw.transform.localPosition = new Vector3(0.02f, 0.14f, 0.02f);
        straw.transform.localRotation = Quaternion.Euler(15f, 0f, 10f);
        straw.transform.localScale = new Vector3(0.015f, 0.08f, 0.015f);
        Destroy(straw.GetComponent<Collider>());
        var strawRenderer = straw.GetComponent<MeshRenderer>();
        if (strawRenderer != null) {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.white;
            strawRenderer.material = mat;
        }

        return sodaContainer;
    }
}