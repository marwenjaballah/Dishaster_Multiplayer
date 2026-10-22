using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A KitchenObject representing an empty, soiled plate left by dining customers.
/// Must be collected by a busser and washed at a SinkCounter before it can be reused.
/// </summary>
public class DirtyPlateKitchenObject : KitchenObject {

    [SerializeField] private GameObject foodScrapsVisual;

    protected override void Awake() {
        base.Awake();
        if (foodScrapsVisual != null) {
            foodScrapsVisual.SetActive(true);
        }
    }
}
