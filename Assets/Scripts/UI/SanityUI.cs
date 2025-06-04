using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class SanityUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider sanitySlider;
    public TextMeshProUGUI sanityText;
    
    private SanitySystem sanitySystem;
    
    void Start()
    {
        // Esperar um pouco para garantir que o player local está spawned
        Invoke(nameof(FindLocalSanitySystem), 1f);
    }
    
    void FindLocalSanitySystem()
    {
        // Encontrar o sistema de sanidade do player local
        SanitySystem[] allSanitySystems = FindObjectsOfType<SanitySystem>();
        foreach (var system in allSanitySystems)
        {
            if (system.IsOwner)
            {
                sanitySystem = system;
                break;
            }
        }
        
        if (sanitySlider != null)
        {
            sanitySlider.maxValue = 100f;
        }
    }
    
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