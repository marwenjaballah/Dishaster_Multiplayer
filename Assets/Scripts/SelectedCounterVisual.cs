using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedCounterVisual : MonoBehaviour {


    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject[] visualGameObjectArray;


    private void Awake() {
        if (baseCounter == null) {
            baseCounter = GetComponentInParent<BaseCounter>();
        }

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform) {
            children.Add(child.gameObject);
        }
        visualGameObjectArray = children.ToArray();

        Hide();
    }

    private void Start() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
            Player.LocalInstance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        } else {
            Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }

        Hide();
    }

    private void Player_OnAnyPlayerSpawned(object sender, System.EventArgs e) {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
            Player.LocalInstance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        }
    }

    private void Player_OnSelectedCounterChanged(object sender, Player.OnSelectedCounterChangedEventArgs e) {
        if (baseCounter != null && e.selectedCounter != null && e.selectedCounter == baseCounter) {
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        if (visualGameObjectArray == null) return;
        foreach (GameObject visualGameObject in visualGameObjectArray) {
            if (visualGameObject != null) {
                visualGameObject.SetActive(true);
            }
        }
    }

    private void Hide() {
        if (visualGameObjectArray == null) return;
        foreach (GameObject visualGameObject in visualGameObjectArray) {
            if (visualGameObject != null) {
                visualGameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
        }
        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
    }
}