using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Portable Pipe Wrench tool.
/// Picked up from the PipeWrenchCounter stand with E.
/// Carried in the player's hands to fix burst water pipes / leaks.
/// Can be returned to the stand or placed on counters with E.
/// </summary>
public class PortablePipeWrench : KitchenObject {

    public static event EventHandler OnAnyWrenchUsed;

    public event EventHandler OnWrenchUsed;

    public void Use(Player player) {
        OnWrenchUsed?.Invoke(this, EventArgs.Empty);
        OnAnyWrenchUsed?.Invoke(this, EventArgs.Empty);
    }
}
