using UnityEngine;


[ExecuteInEditMode]
public class GetMainLightDirection : MonoBehaviour
{
    [SerializeField]private Material skyboxMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        skyboxMaterial.SetVector("_LightDir", transform.forward);
        skyboxMaterial.SetVector("_MainLightUp", transform.up);
        skyboxMaterial.SetVector("_MainLightRight", transform.right);
    }
}
