using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// On-screen testing and debug panel for Kitchen Crisis Events.
/// Allows rapid triggering of Blackouts, Fires, Resets, and Auto-Event modes.
/// </summary>
public class CrisisDebugUI : MonoBehaviour {

    [SerializeField] private bool showDebugUI = false;

    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private bool stylesInitialized = false;

    private void InitStyles() {
        if (stylesInitialized) return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 13;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.yellow;

        stylesInitialized = true;
    }

    private void OnGUI() {
        if (!showDebugUI) return;
        if (KitchenCrisisManager.Instance == null) return;

        InitStyles();

        float width = 230f;
        float height = 275f;
        float x = Screen.width - width - 20f;
        float y = 20f;

        GUILayout.BeginArea(new Rect(x, y, width, height), "🚨 CRISIS TEST PANEL", boxStyle);
        GUILayout.Space(25f);

        // Status
        bool isBlackout = KitchenCrisisManager.Instance.IsBlackout();
        bool isWaterLeak = KitchenCrisisManager.Instance.IsWaterLeakActive();
        int activeFires = KitchenCrisisManager.Instance.ActiveFireCount;
        bool isAuto = KitchenCrisisManager.Instance.IsAutoCrisisEnabled();

        GUILayout.Label($"Power: {(isBlackout ? "<color=red>⚡ BLACKOUT</color>" : "<color=green>⚡ OK</color>")}", labelStyle);
        GUILayout.Label($"Plumbing: {(isWaterLeak ? "<color=cyan>💧 LEAKING</color>" : "<color=green>💧 OK</color>")}", labelStyle);
        GUILayout.Label($"Fires: {(activeFires > 0 ? $"<color=orange>{activeFires} 🔥</color>" : "0")}", labelStyle);

        GUILayout.Space(6f);

        // Trigger Blackout Button
        GUI.backgroundColor = isBlackout ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button(isBlackout ? "⚡ RESTORE POWER" : "⚡ TRIGGER BLACKOUT", buttonStyle, GUILayout.Height(28))) {
            if (isBlackout) {
                KitchenCrisisManager.Instance.EndBlackoutServerRpc();
            } else {
                KitchenCrisisManager.Instance.TriggerBlackoutServerRpc();
            }
        }

        // Trigger Water Leak Button
        GUI.backgroundColor = isWaterLeak ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.1f, 0.7f, 0.9f);
        if (GUILayout.Button(isWaterLeak ? "💧 FIX WATER LEAK" : "💧 TRIGGER WATER LEAK", buttonStyle, GUILayout.Height(28))) {
            if (isWaterLeak) {
                KitchenCrisisManager.Instance.EndWaterLeakServerRpc();
            } else {
                KitchenCrisisManager.Instance.TriggerWaterLeakServerRpc();
            }
        }

        // Trigger Fire Button
        GUI.backgroundColor = new Color(1f, 0.4f, 0.2f);
        if (GUILayout.Button("🔥 SPAWN STOVE FIRE", buttonStyle, GUILayout.Height(28))) {
            KitchenCrisisManager.Instance.TriggerFireServerRpc();
        }

        // Reset All Button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🧯 RESET ALL CRISES", buttonStyle, GUILayout.Height(26))) {
            KitchenCrisisManager.Instance.ResetAllCrisesServerRpc();
        }

        // Toggle Auto Mode Button
        GUI.backgroundColor = isAuto ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.5f, 0.5f, 0.5f);
        if (GUILayout.Button($"⏱️ AUTO CRISIS: {(isAuto ? "ON" : "OFF")}", buttonStyle, GUILayout.Height(24))) {
            KitchenCrisisManager.Instance.ToggleAutoCrisisServerRpc();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndArea();
    }
}
