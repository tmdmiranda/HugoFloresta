using System;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interaction : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInputHandler playerInputHandler;

    [Header("Interact Parameters")]
    [SerializeField] private Transform interactPoint;
    [SerializeField] private float interactPointRadius = 0.5f;
    [SerializeField] private LayerMask interactableMask;

    private readonly Collider[] _colliders = new Collider[3];
    [SerializeField] private int _colliderCount;

    public bool isInteracting = false;

    private void Update()
    {
        _colliderCount = Physics.OverlapSphereNonAlloc( interactPoint.position, interactPointRadius, _colliders, interactableMask);

        if (_colliderCount != 0)
        {
            var ineractable = _colliders[0].GetComponent<EnterCar>();

            //if (ineractable != null && playerInputHandler.InteractTriggered)
            if (ineractable != null && Keyboard.current.eKey.wasPressedThisFrame )
            {
                Debug.Log("Interacting");
                ineractable.Interact(this);
                isInteracting = true;
            }
        }
    }

    //mostras as linhas vermelhas no editor de scene 
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(interactPoint.position, interactPointRadius);
    }
}
