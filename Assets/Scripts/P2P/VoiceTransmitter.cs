using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class VoiceTransmitter : NetworkBehaviour
{
    private AudioClip micClip;
    private const int sampleRate = 16000;
    private const int chunkSize = 256; // Smaller to avoid Overflow
    private string micDevice;
    private int lastSamplePos;
    private float[] audioBuffer = new float[chunkSize];
    private VoiceReceiver receiver;

    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            micDevice = Microphone.devices.FirstOrDefault();
            if (micDevice != null)
            {
                micClip = Microphone.Start(micDevice, true, 1, sampleRate);
                Debug.Log("Microphone started: " + micDevice);
            }
            else
            {
                Debug.LogError("No microphone detected.");
            }
        }
    }

    private void Update()
    {
        if (!IsOwner || micClip == null) return;

        if (!Input.GetKey(pushToTalkKey)) return;

        int micPos = Microphone.GetPosition(null);
        if (micPos < chunkSize) return;

        float[] samples = new float[micClip.samples * micClip.channels];
        micClip.GetData(samples, 0);

        int start = Mathf.Max(0, micPos - chunkSize);
        float[] slice = new float[chunkSize];
        Array.Copy(samples, start, slice, 0, chunkSize);

        Debug.Log($"[VoiceTransmitter] Sending voice data (Pos: {micPos})");
        SendVoiceServerRpc(slice);
    }


    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SendVoiceServerRpc(float[] data, ServerRpcParams rpcParams = default)
    {
        ReceiveVoiceClientRpc(data, OwnerClientId);
    }

    [ClientRpc]
    private void ReceiveVoiceClientRpc(float[] data, ulong senderId)
    {
        if (IsOwner) return;

        if (receiver == null)
        {
            receiver = GetComponent<VoiceReceiver>();
            if (receiver == null)
                receiver = gameObject.AddComponent<VoiceReceiver>();
        }

        receiver.PlayClip(data);
    }
}
