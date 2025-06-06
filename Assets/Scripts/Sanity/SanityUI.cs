using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class SanityUI : NetworkBehaviour
{
    [Header("UI References")]
    public Slider sanitySlider;
    public TextMeshProUGUI sanityText;
    
    public SanitySystem sanitySystem;
    
    void Update()
    {
        if (sanitySystem != null)
        {
            float currentSanity = sanitySystem.GetSanity();

            if (sanitySlider != null)
            {
                sanitySlider.value = currentSanity;
            }

            if (sanityText != null)
            {
                sanityText.text = $"Sanity: {currentSanity:F0}/100";
            }
        }
    }
}