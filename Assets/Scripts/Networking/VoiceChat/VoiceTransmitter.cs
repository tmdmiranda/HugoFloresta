using UnityEngine;
using Unity.Netcode;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoiceTransmitter : NetworkBehaviour
{
    [Header("Settings")]
    public int sampleRate = 16000;
    public int chunkSize = 1024;
    public KeyCode pushToTalkKey = KeyCode.V;
    public float voiceVolume = 1f;

    private AudioClip micClip;
    private string micDevice;
    private bool isTransmitting;
    private int lastSamplePos;
    private float[] audioBuffer;

    private void Start()
    {
        if (!IsOwner) return;

        audioBuffer = new float[chunkSize];
        micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

        if (micDevice != null)
        {
            micClip = Microphone.Start(micDevice, true, 1, sampleRate);
            lastSamplePos = Microphone.GetPosition(micDevice);
            Debug.Log($"[Transmitter] Microphone started: {micDevice}, SampleRate: {sampleRate}");
        }
        else
        {
            Debug.LogError("[Transmitter] No microphone detected!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner || micClip == null) return;

        if (Input.GetKeyDown(pushToTalkKey)) isTransmitting = true;
        if (Input.GetKeyUp(pushToTalkKey)) isTransmitting = false;

        if (!isTransmitting) return;

        int currentPos = Microphone.GetPosition(micDevice);
        int samplesAvailable = currentPos - lastSamplePos;

        if (samplesAvailable < 0)
            samplesAvailable += micClip.samples;

        if (samplesAvailable >= chunkSize)
        {
            TransmitVoiceChunk(currentPos);
        }
    }

    private void TransmitVoiceChunk(int currentPos)
    {
        micClip.GetData(audioBuffer, lastSamplePos);
        lastSamplePos = (lastSamplePos + chunkSize) % micClip.samples;

        for (int i = 0; i < audioBuffer.Length; i++)
        {
            audioBuffer[i] *= voiceVolume;
        }

        byte[] byteData = new byte[audioBuffer.Length * sizeof(float)];
        Buffer.BlockCopy(audioBuffer, 0, byteData, 0, byteData.Length);

        Debug.Log($"[Transmitter] Sending {byteData.Length} bytes of audio data.");
        SendVoiceDataServerRpc(byteData);
    }

    public void SetMicrophoneDevice(string deviceName)
    {
        if (Microphone.IsRecording(micDevice))
        {
            Microphone.End(micDevice);
        }

        micDevice = deviceName;
        micClip = Microphone.Start(micDevice, true, 1, sampleRate);
        lastSamplePos = Microphone.GetPosition(micDevice);
        Debug.Log($"Switched to mic: {micDevice}");
    }

    [ServerRpc]
    private void SendVoiceDataServerRpc(byte[] voiceData, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[Server] Relaying {voiceData.Length} bytes from client {rpcParams.Receive.SenderClientId}");
        ReceiveVoiceDataClientRpc(voiceData, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ReceiveVoiceDataClientRpc(byte[] voiceData, ulong senderId)
    {
        if (IsOwner) return;

        float[] audioData = new float[voiceData.Length / sizeof(float)];
        Buffer.BlockCopy(voiceData, 0, audioData, 0, voiceData.Length);
        Debug.Log($"[Client Receiver] Received {audioData.Length} float samples from client {senderId}");

        var receiver = GetComponent<VoiceReceiver>() ?? gameObject.AddComponent<VoiceReceiver>();
        receiver.AddAudioData(audioData, sampleRate);
    }

    private void OnDestroy()
    {
        if (micClip != null)
        {
            Microphone.End(micDevice);
            Destroy(micClip);
        }
    }
}
