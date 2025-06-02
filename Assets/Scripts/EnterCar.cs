using UnityEngine;

public class EnterCar : MonoBehaviour
{
    [SerializeField] private string text;

    public string interactionText => text;

    public bool Interact(Interaction interactor)
    {
        Debug.Log(text);
        return true;
    }
}
