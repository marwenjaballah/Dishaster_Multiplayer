using System;
using UnityEngine;

/// <summary>
/// Visual and audio presentation for the PassThroughWindowCounter.
/// Controls the warm heat-lamp overhead lighting and the Order Up! bell chime.
/// </summary>
public class PassThroughWindowVisual : MonoBehaviour
{
    [SerializeField] private PassThroughWindowCounter passCounter;
    [SerializeField] private Light heatLampLight;
    [SerializeField] private GameObject orderUpIndicator;
    [SerializeField] private AudioSource bellAudioSource;

    private void Awake()
    {
        if (passCounter == null)
        {
            passCounter = GetComponentInParent<PassThroughWindowCounter>();
        }
    }

    private void Start()
    {
        if (passCounter != null)
        {
            passCounter.OnOrderPlaced += PassCounter_OnOrderPlaced;
            passCounter.OnOrderPickedUp += PassCounter_OnOrderPickedUp;
        }

        UpdateVisuals(false);
    }

    private void OnDestroy()
    {
        if (passCounter != null)
        {
            passCounter.OnOrderPlaced -= PassCounter_OnOrderPlaced;
            passCounter.OnOrderPickedUp -= PassCounter_OnOrderPickedUp;
        }
    }

    private void PassCounter_OnOrderPlaced(object sender, EventArgs e)
    {
        UpdateVisuals(true);
        PlayBellChime();
    }

    private void PassCounter_OnOrderPickedUp(object sender, EventArgs e)
    {
        UpdateVisuals(false);
    }

    private void UpdateVisuals(bool hasOrder)
    {
        if (heatLampLight != null)
        {
            heatLampLight.enabled = hasOrder;
            if (hasOrder)
            {
                heatLampLight.color = new Color(1f, 0.7f, 0.3f);
            }
        }

        if (orderUpIndicator != null)
        {
            orderUpIndicator.SetActive(hasOrder);
        }
    }

    private void PlayBellChime()
    {
        if (bellAudioSource != null && bellAudioSource.clip != null)
        {
            bellAudioSource.Play();
        }
    }
}
