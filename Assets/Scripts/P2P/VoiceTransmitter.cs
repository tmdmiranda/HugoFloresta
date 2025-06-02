using UnityEngine;
using Unity.Netcode;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoiceTransmitter : NetworkBehaviour
{

    [Header("Voice Settings")]
    [Range(0.1f, 1f)] public float voiceVolume = 0.8f;
    [Range(0.05f, 0.2f)] public float transmissionInterval = 0.1f;
    public float maxHearingDistance = 20f;
    private AudioClip micClip;
    private const int sampleRate = 44100; // Increased for better quality
    private const int chunkSize = 2048; // Larger buffer for stability
    private string micDevice;
    private bool isTransmitting;
    private float[] audioBuffer = new float[chunkSize];
    private int lastSamplePos;

    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V; 
    private float nextTransmitTime;

    private void Start()
    {
        if (IsOwner)
        {
            micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
            if (micDevice != null)
            {
                micClip = Microphone.Start(micDevice, true, 1, sampleRate);
                Debug.Log($"Microphone started: {micDevice}, Sample rate: {sampleRate}");
            }
            else
            {
                Debug.LogWarning("No microphone detected!");
            }
        }
    }

    private void Update()
    {
        if (!IsOwner || micClip == null) return;

        // Toggle transmission state
        if (Input.GetKeyDown(pushToTalkKey)) isTransmitting = true;
        if (Input.GetKeyUp(pushToTalkKey)) isTransmitting = false;

        // Transmit at intervals when key is held
        if (isTransmitting && Time.time >= nextTransmitTime)
        {
            TransmitVoiceChunk();
            nextTransmitTime = Time.time + transmissionInterval;
        }
    }

    private void TransmitVoiceChunk()
    {
        int currentPos = Microphone.GetPosition(micDevice);
        if (currentPos <= lastSamplePos) return;

        // Get new samples since last transmission
        int sampleCount = currentPos - lastSamplePos;
        if (sampleCount <= 0) return;

        // Ensure we don't exceed buffer size
        sampleCount = Mathf.Min(sampleCount, chunkSize);
        float[] samples = new float[sampleCount];
        micClip.GetData(samples, lastSamplePos);

        // Apply volume
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= voiceVolume;
        }

        // Send compressed data
        byte[] compressed = CompressAudio(samples);
        SendVoiceServerRpc(compressed);

        lastSamplePos = currentPos;
    }

    private byte[] CompressAudio(float[] samples)
    {
        // Simple compression (for real projects use proper codec)
        byte[] bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    [ServerRpc]
    private void SendVoiceServerRpc(byte[] compressedData, ServerRpcParams rpcParams = default)
    {
        ReceiveVoiceClientRpc(compressedData, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ReceiveVoiceClientRpc(byte[] compressedData, ulong senderId)
    {
        if (IsOwner) return;

        float[] samples = DecompressAudio(compressedData);
        VoiceReceiver receiver = GetComponent<VoiceReceiver>();
        if (receiver == null) receiver = gameObject.AddComponent<VoiceReceiver>();
        receiver.PlayVoiceClip(samples, sampleRate);
    }

    private float[] DecompressAudio(byte[] bytes)
    {
        float[] samples = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
        return samples;
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