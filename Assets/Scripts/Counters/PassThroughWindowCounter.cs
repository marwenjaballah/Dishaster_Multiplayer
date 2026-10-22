using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Dual-sided service pass counter bridging the kitchen cook line and the dining/delivery zone.
/// Features warm heat-lamp holding, audible 'Order Up!' bell ding, and team broadcast.
/// </summary>
public class PassThroughWindowCounter : BaseCounter
{
    public static event EventHandler<OnPassOrderEventArgs> OnAnyPassOrderPlaced;
    public static event EventHandler OnAnyPassOrderPickedUp;

    new public static void ResetStaticData()
    {
        OnAnyPassOrderPlaced = null;
        OnAnyPassOrderPickedUp = null;
    }

    public class OnPassOrderEventArgs : EventArgs
    {
        public KitchenObjectSO kitchenObjectSO;
        public Vector3 position;
    }

    public event EventHandler OnOrderPlaced;
    public event EventHandler OnOrderPickedUp;

    private bool _hasOrderWaiting;
    private float _holdingTimer;

    public bool HasOrderWaiting => _hasOrderWaiting;
    public float HoldingTimer => _holdingTimer;

    private void Update()
    {
        if (_hasOrderWaiting)
        {
            _holdingTimer += Time.deltaTime;
        }
    }

    public override void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                // Disallow emergency tools from being placed on the food pass
                if (player.GetKitchenObject() is PortableFireExtinguisher || player.GetKitchenObject() is PortablePipeWrench)
                {
                    return;
                }

                KitchenObject heldObj = player.GetKitchenObject();
                heldObj.SetKitchenObjectParent(this);
                InteractLogicPlaceServerRpc();
            }
        }
        else
        {
            if (!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
                InteractLogicPickupServerRpc();
            }
            else
            {
                // Bag packing logic: Counter has bag, Player has plate
                if (GetKitchenObject() is DeliveryBagKitchenObject bagOnCounter && player.GetKitchenObject().TryGetPlate(out PlateKitchenObject heldPlate))
                {
                    if (bagOnCounter.CanAddPlate && heldPlate.GetKitchenObjectSOList().Count > 0)
                    {
                        if (bagOnCounter.TryAddPlate(heldPlate))
                        {
                            return;
                        }
                    }
                }

                // Bag packing logic: Player has bag, Counter has plate
                if (player.GetKitchenObject() is DeliveryBagKitchenObject heldBag && GetKitchenObject().TryGetPlate(out PlateKitchenObject counterPlate))
                {
                    if (heldBag.CanAddPlate && counterPlate.GetKitchenObjectSOList().Count > 0)
                    {
                        if (heldBag.TryAddPlate(counterPlate))
                        {
                            InteractLogicPickupServerRpc();
                            return;
                        }
                    }
                }

                // Ingredient plating logic
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject playerPlate))
                {
                    if (playerPlate.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        InteractLogicPickupServerRpc();
                    }
                }
                else if (GetKitchenObject().TryGetPlate(out PlateKitchenObject passPlate))
                {
                    if (passPlate.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicPlaceServerRpc()
    {
        OrderPlacedClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OrderPlacedClientRpc()
    {
        _hasOrderWaiting = true;
        _holdingTimer = 0f;
        OnOrderPlaced?.Invoke(this, EventArgs.Empty);

        KitchenObjectSO objSO = HasKitchenObject() ? GetKitchenObject().GetKitchenObjectSO() : null;
        OnAnyPassOrderPlaced?.Invoke(this, new OnPassOrderEventArgs
        {
            kitchenObjectSO = objSO,
            position = transform.position
        });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicPickupServerRpc()
    {
        OrderPickedUpClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OrderPickedUpClientRpc()
    {
        _hasOrderWaiting = false;
        _holdingTimer = 0f;
        OnOrderPickedUp?.Invoke(this, EventArgs.Empty);
        OnAnyPassOrderPickedUp?.Invoke(this, EventArgs.Empty);
    }
}
