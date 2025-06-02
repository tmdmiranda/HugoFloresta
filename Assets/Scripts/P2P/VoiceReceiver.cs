using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class VoiceReceiver : MonoBehaviour
{
    [Header("Settings")]
    public float playbackVolume = 0.8f;
    public float bufferDuration = 0.3f;
    public float minBufferToPlay = 0.05f;

    private AudioSource audioSource;
    private List<float> sampleBuffer = new List<float>();
    private int sampleRate;
    private bool isPlaying;
    private float lastAddTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = playbackVolume;

        Debug.Log("[Receiver] AudioSource initialized.");
    }

    public void AddAudioData(float[] samples, int incomingSampleRate)
    {
        if (samples == null || samples.Length == 0)
        {
            Debug.LogWarning("[Receiver] Received empty or null sample data.");
            return;
        }

        sampleRate = incomingSampleRate;
        lastAddTime = Time.time;

        lock (sampleBuffer)
        {
            sampleBuffer.AddRange(samples);
            Debug.Log($"[Receiver] Added {samples.Length} samples. Buffer now: {sampleBuffer.Count}");

            int maxSamples = Mathf.FloorToInt(bufferDuration * sampleRate);
            while (sampleBuffer.Count > maxSamples)
                sampleBuffer.RemoveAt(0);
        }

        if (!isPlaying && GetBufferedDuration() >= minBufferToPlay)
        {
            Debug.Log("[Receiver] Enough buffered data. Starting playback.");
            PlayAudio();
        }
    }

    private void PlayAudio()
    {
        float[] playbackData;
        lock (sampleBuffer)
        {
            playbackData = sampleBuffer.ToArray();
            sampleBuffer.Clear();
        }

        if (playbackData.Length == 0)
        {
            Debug.LogWarning("[Receiver] Tried to play empty buffer.");
            return;
        }

        AudioClip clip = AudioClip.Create("VoiceClip", playbackData.Length, 1, sampleRate, false);
        clip.SetData(playbackData, 0);

        audioSource.clip = clip;
        audioSource.Play();
        isPlaying = true;

        Debug.Log($"[Receiver] Playing audio clip. Length: {clip.length}s");

        Invoke(nameof(OnClipFinished), clip.length);
    }

    private void OnClipFinished()
    {
        isPlaying = false;
        Debug.Log("[Receiver] Audio clip finished.");

        if (GetBufferedDuration() >= minBufferToPlay)
        {
            Debug.Log("[Receiver] More buffered data available. Playing next.");
            PlayAudio();
        }
    }

    private float GetBufferedDuration()
    {
        float duration = sampleRate == 0 ? 0f : (float)sampleBuffer.Count / sampleRate;
        Debug.Log($"[Receiver] Buffered duration: {duration:0.000}s");
        return duration;
    }

    private void Update()
    {
        if (isPlaying && Time.time - lastAddTime > 1f)
        {
            Debug.LogWarning("[Receiver] Timeout detected. Resetting.");
            audioSource.Stop();
            sampleBuffer.Clear();
            isPlaying = false;
        }
    }
}
