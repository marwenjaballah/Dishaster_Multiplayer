using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearCounter : BaseCounter {


    [SerializeField] private KitchenObjectSO kitchenObjectSO;


    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // There is no KitchenObject here
            if (player.HasKitchenObject()) {
                // Emergency tools must be returned to their designated wall mounts, not left on food counters
                if (player.GetKitchenObject() is PortableFireExtinguisher || player.GetKitchenObject() is PortablePipeWrench) {
                    return;
                }

                // Player is carrying something
                player.GetKitchenObject().SetKitchenObjectParent(this);
            } else {
                // Player not carrying anything
            }
        } else {
            // There is a KitchenObject here
            if (player.HasKitchenObject()) {
                // Player is carrying something
                // Check 1: Counter has DeliveryBag, Player has Plate
                if (GetKitchenObject() is DeliveryBagKitchenObject bagOnCounter && player.GetKitchenObject().TryGetPlate(out PlateKitchenObject heldPlate)) {
                    if (bagOnCounter.CanAddPlate && heldPlate.GetKitchenObjectSOList().Count > 0) {
                        if (bagOnCounter.TryAddPlate(heldPlate)) {
                            KitchenObject.DestroyKitchenObject(heldPlate);
                            return;
                        }
                    }
                }

                // Check 2: Player has DeliveryBag, Counter has Plate
                if (player.GetKitchenObject() is DeliveryBagKitchenObject heldBag && GetKitchenObject().TryGetPlate(out PlateKitchenObject counterPlate)) {
                    if (heldBag.CanAddPlate && counterPlate.GetKitchenObjectSOList().Count > 0) {
                        if (heldBag.TryAddPlate(counterPlate)) {
                            KitchenObject.DestroyKitchenObject(counterPlate);
                            return;
                        }
                    }
                }

                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    // Player is holding a Plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                } else {
                    // Player is not carrying Plate but something else
                    if (GetKitchenObject().TryGetPlate(out plateKitchenObject)) {
                        // Counter is holding a Plate
                        if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO())) {
                            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
                        }
                    }
                }
            } else {
                // Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }

}