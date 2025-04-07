using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCamera : MonoBehaviour 
{
    public Transform playerCamera;
    public Transform window;
    public Transform otherRoomWindow;
    
    void LateUpdate() 
    {
        // Calculate position relative to the other window
        Vector3 relativePos = otherRoomWindow.InverseTransformPoint(playerCamera.position);
        transform.position = window.TransformPoint(relativePos);
        
        // Calculate rotation difference between windows
        Quaternion relativeRot = Quaternion.Inverse(otherRoomWindow.rotation) * playerCamera.rotation;
        transform.rotation = window.rotation * relativeRot;
        
        // Adjust camera clipping to prevent visual artifacts
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Ensure near clip plane is appropriate for the window distance
            float distanceToWindow = Vector3.Distance(transform.position, window.position);
            cam.nearClipPlane = Mathf.Max(0.01f, distanceToWindow * 0.1f);
        }
    }
}
