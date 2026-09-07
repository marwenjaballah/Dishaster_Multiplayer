using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A counter placed in the kitchen to dispense Delivery Bags and pack plated meals into bags.
/// </summary>
public class DeliveryBagCounter : BaseCounter
{
    public event EventHandler OnBagDispensed;

    [SerializeField] private KitchenObjectSO deliveryBagKitchenObjectSO;

    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            // Player is empty handed
            if (!HasKitchenObject())
            {
                // Counter is empty -> Spawn new DeliveryBag to player
                if (deliveryBagKitchenObjectSO != null)
                {
                    KitchenObject.SpawnKitchenObject(deliveryBagKitchenObjectSO, player);
                    OnBagDispensed?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                // Counter has an object -> Give to player
                GetKitchenObject().SetKitchenObjectParent(player);
                OnBagDispensed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Player has an object
            KitchenObject held = player.GetKitchenObject();

            // Scenario 1: Player holds a Plate, Counter holds a DeliveryBag
            if (held is PlateKitchenObject plate && HasKitchenObject() && GetKitchenObject() is DeliveryBagKitchenObject bagOnCounter)
            {
                if (bagOnCounter.CanAddPlate && plate.GetKitchenObjectSOList().Count > 0)
                {
                    if (bagOnCounter.TryAddPlate(plate))
                    {
                        KitchenObject.DestroyKitchenObject(plate);
                    }
                }
            }
            // Scenario 2: Player holds a DeliveryBag, Counter holds a Plate
            else if (held is DeliveryBagKitchenObject playerBag && HasKitchenObject() && GetKitchenObject() is PlateKitchenObject plateOnCounter)
            {
                if (playerBag.CanAddPlate && plateOnCounter.GetKitchenObjectSOList().Count > 0)
                {
                    if (playerBag.TryAddPlate(plateOnCounter))
                    {
                        KitchenObject.DestroyKitchenObject(plateOnCounter);
                    }
                }
            }
            // Scenario 3: Player holds a DeliveryBag or Plate and counter is empty -> place on counter
            else if (!HasKitchenObject())
            {
                held.SetKitchenObjectParent(this);
            }
        }
    }
}
