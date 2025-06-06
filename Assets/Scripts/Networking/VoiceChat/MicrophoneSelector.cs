using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;
using UnityEngine.UI;
// This script allows the user to select a microphone from a dropdown menu and apply it to the VoiceTransmitter component.

public class MicrophoneSelector : MonoBehaviour
{
    public TMP_Dropdown micDropdown; // Assign in Inspector
    private VoiceTransmitter voiceTransmitter;

    private List<string> micDevices = new List<string>();

    private void Start()
    {

        StartCoroutine(DelayedAssignVoiceTransmitter());

        RefreshMicrophones();
    }


    private IEnumerator DelayedAssignVoiceTransmitter()
    {
        yield return new WaitForSeconds(5f); // wait 5 seconds
        AssignLocalPlayerVoiceTransmitter();
    }

    private void AssignLocalPlayerVoiceTransmitter()
    {
        Camera[] cameras = Camera.allCameras;

        foreach (var cam in cameras)
        {
            if (cam.gameObject.activeInHierarchy && cam.enabled)
            {
                GameObject playerGO = cam.gameObject;

                if (playerGO.GetComponent<VoiceTransmitter>() == null && playerGO.transform.parent != null)
                {
                    playerGO = playerGO.transform.parent.gameObject;
                }

                voiceTransmitter = playerGO.GetComponent<VoiceTransmitter>();

                if (voiceTransmitter != null)
                {
                    Debug.Log($"Found VoiceTransmitter on local player via active camera: {playerGO.name}");
                }
                else
                {
                    Debug.LogWarning("No VoiceTransmitter found on the object with active camera.");
                }
                return;
            }
        }

        Debug.LogError("No active camera found in scene to find local player.");
    }



    void RefreshMicrophones()
    {
        micDropdown.ClearOptions();
        micDevices = new List<string>(Microphone.devices);

        if (micDevices.Count == 0)
        {
            micDevices.Add("No microphone found");
            micDropdown.interactable = false;
        }
        else
        {
            micDropdown.interactable = true;
        }

        micDropdown.AddOptions(micDevices);
    }

    public void ApplyMicrophone()
    {
        if (voiceTransmitter == null)
        {
            Debug.LogError("No VoiceTransmitter assigned.");
            return;
        }

        if (micDevices.Count == 0) return;

        string selectedDevice = micDevices[micDropdown.value];
        voiceTransmitter.SetMicrophoneDevice(selectedDevice);
    }
}
