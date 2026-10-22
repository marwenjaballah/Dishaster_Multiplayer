using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative Co-op Ping & Emote system.
/// Allows players to broadcast rapid context-aware tactical pings to teammates:
/// ("Need Plates!", "Order Up!", "Going City!", "Fire / Emergency!", "Need Help!").
/// Spawns 3D in-world beacons, audio alerts, and HUD toasts for all connected players.
/// </summary>
public class CoopPingManager : NetworkBehaviour {

    public static CoopPingManager Instance { get; private set; }

    public enum PingType {
        NeedPlates,      // 🍽️ "Need Plates!"
        OrderUp,         // 🔔 "Order Up!"
        GoingCity,       // 🛵 "Going City!"
        EmergencyFire,   // 🔥 "Emergency / Fire!"
        NeedHelp,        // 🆘 "Need Help Here!"
        ThankYou         // 👍 "Thank You!"
    }

    public class OnPingEventArgs : EventArgs {
        public PingType pingType;
        public Vector3 worldPosition;
        public ulong senderClientId;
        public string senderPlayerName;
        public Color senderColor;
        public string messageText;
    }

    public static event EventHandler<OnPingEventArgs> OnAnyPingReceived;

    public static void ResetStaticData() {
        OnAnyPingReceived = null;
    }

    [Header("Ping Visual Assets")]
    [SerializeField] private GameObject worldPingIndicatorPrefab;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy() {
        base.OnDestroy();
        if (Instance == this) {
            Instance = null;
        }
    }

    /// <summary>
    /// Initiates a ping from the local player.
    /// </summary>
    public void SendPing(PingType pingType, Vector3 worldPosition) {
        if (!KitchenGameManager.Instance.IsGamePlaying() && !KitchenGameManager.Instance.IsCountdownToStartActive()) {
            return;
        }

        SendPingServerRpc(pingType, worldPosition);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendPingServerRpc(PingType pingType, Vector3 worldPosition, RpcParams rpcParams = default) {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        string senderName = "Player";
        int colorId = 0;

        if (KitchenGameMultiplayer.Instance != null) {
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(senderClientId);
            if (playerData.clientId == senderClientId) {
                senderName = playerData.playerName.ToString();
                colorId = playerData.colorId;
            }
        }


        SendPingClientRpc(pingType, worldPosition, senderClientId, senderName, colorId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendPingClientRpc(PingType pingType, Vector3 worldPosition, ulong senderClientId, string senderName, int colorId) {
        Color senderColor = Color.white;
        if (KitchenGameMultiplayer.Instance != null) {
            senderColor = KitchenGameMultiplayer.Instance.GetPlayerColor(colorId);
        }

        string messageText = GetPingDescription(pingType);

        // Spawn 3D in-world beacon
        SpawnWorldPingBeacon(pingType, worldPosition, senderName, senderColor);

        // Play SFX
        PlayPingSound(pingType, worldPosition);

        // Invoke event for HUD UI toast notifications
        OnAnyPingReceived?.Invoke(this, new OnPingEventArgs {
            pingType = pingType,
            worldPosition = worldPosition,
            senderClientId = senderClientId,
            senderPlayerName = senderName,
            senderColor = senderColor,
            messageText = messageText
        });
    }

    private void SpawnWorldPingBeacon(PingType pingType, Vector3 position, string senderName, Color color) {
        GameObject beaconGo = new GameObject($"WorldPing_{pingType}");
        beaconGo.transform.position = position + Vector3.up * 0.8f;

        WorldPingIndicator indicator = beaconGo.AddComponent<WorldPingIndicator>();
        indicator.Initialize(pingType, senderName, color);
    }

    private void PlayPingSound(PingType pingType, Vector3 position) {
        if (SoundManager.Instance == null) return;

        // SoundManager has warning, deliverySuccess, objectPickup, etc.
        // We trigger sound through SoundManager reflections or fallback audio source
        var audioRefProp = typeof(SoundManager).GetField("audioClipRefsSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (audioRefProp != null) {
            AudioClipRefsSO refs = audioRefProp.GetValue(SoundManager.Instance) as AudioClipRefsSO;
            if (refs != null) {
                AudioClip clipToPlay = null;
                switch (pingType) {
                    case PingType.EmergencyFire:
                        if (refs.warning != null && refs.warning.Length > 0) clipToPlay = refs.warning[0];
                        break;
                    case PingType.OrderUp:
                        if (refs.deliverySuccess != null && refs.deliverySuccess.Length > 0) clipToPlay = refs.deliverySuccess[0];
                        break;
                    case PingType.NeedPlates:
                    case PingType.GoingCity:
                    case PingType.NeedHelp:
                    case PingType.ThankYou:
                    default:
                        if (refs.objectPickup != null && refs.objectPickup.Length > 0) clipToPlay = refs.objectPickup[0];
                        break;
                }

                if (clipToPlay != null) {
                    AudioSource.PlayClipAtPoint(clipToPlay, position, 1.2f);
                }
            }
        }
    }

    public static string GetPingDescription(PingType type) {
        switch (type) {
            case PingType.NeedPlates:    return "Need Clean Plates!";
            case PingType.OrderUp:       return "Order Up at Pass!";
            case PingType.GoingCity:     return "Heading to City!";
            case PingType.EmergencyFire: return "Crisis / Fire Alert!";
            case PingType.NeedHelp:      return "Need Assistance!";
            case PingType.ThankYou:      return "Great Job / Thanks!";
            default:                     return "Ping!";
        }
    }

    public static string GetPingEmoji(PingType type) {
        switch (type) {
            case PingType.NeedPlates:    return "[PLATES]";
            case PingType.OrderUp:       return "[ORDER UP]";
            case PingType.GoingCity:     return "[COURIER]";
            case PingType.EmergencyFire: return "[FIRE!]";
            case PingType.NeedHelp:      return "[HELP!]";
            case PingType.ThankYou:      return "[THANKS!]";
            default:                     return "[PING]";
        }
    }

}
