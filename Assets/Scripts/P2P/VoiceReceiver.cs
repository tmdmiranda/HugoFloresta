using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceReceiver : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip currentClip;
    private int samplePos;
    private float[] sampleBuffer = new float[2048];

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D audio
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayVoiceClip(float[] samples, int frequency)
    {
        if (samples == null || samples.Length == 0) return;

        // Create or update clip
        if (currentClip == null || currentClip.frequency != frequency)
        {
            currentClip = AudioClip.Create("VoiceClip", samples.Length, 1, frequency, false);
        }

        currentClip.SetData(samples, 0);
        audioSource.clip = currentClip;
        audioSource.Play();
    }
}