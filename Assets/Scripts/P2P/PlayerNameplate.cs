using TMPro;
using UnityEngine;

public class PlayerNameplate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private Vector3 rotationOffset = new Vector3(45f, 0f, 0f);
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Material fontMaterial;
    [SerializeField] private float fontSize = 3f;
    [SerializeField] private Color textColor = Color.white;

    private TextMeshPro nameplateText;
    private Camera mainCamera;

    private void Awake()
    {
        CreateWorldSpaceText();
        mainCamera = Camera.main;
    }

    private void CreateWorldSpaceText()
    {
        // Create new GameObject for the text
        GameObject textGO = new GameObject("PlayerNameplate");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = Vector3.up * verticalOffset;
        textGO.transform.localScale = Vector3.one * 0.1f; // Scale down if needed

        // Add TextMeshPro component
        nameplateText = textGO.AddComponent<TextMeshPro>();
        
        // Configure text properties
        nameplateText.font = fontAsset;
        nameplateText.fontSharedMaterial = fontMaterial;
        nameplateText.fontSize = fontSize;
        nameplateText.color = textColor;
        nameplateText.alignment = TextAlignmentOptions.Center;
        nameplateText.enableWordWrapping = false;
        
    }

    private void LateUpdate()
    {
        if (mainCamera == null || nameplateText == null) return;
        
        // Make text face the camera (billboard effect)
        nameplateText.transform.rotation = mainCamera.transform.rotation;
        nameplateText.transform.Rotate(rotationOffset);
    }

    public void SetName(string playerName)
    {
        if (nameplateText != null)
        {
            nameplateText.text = playerName;
            
            // Auto-resize for long names
            if (playerName.Length > 12)
            {
                nameplateText.autoSizeTextContainer = true;
                nameplateText.fontSizeMax = fontSize;
                nameplateText.fontSizeMin = fontSize * 0.7f;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (nameplateText != null)
        {
            Destroy(nameplateText.gameObject);
        }
    }
}