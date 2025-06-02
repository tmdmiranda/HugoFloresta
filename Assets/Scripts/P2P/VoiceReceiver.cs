using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceReceiver : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D audio
    }

    public void PlayClip(float[] data)
    {
        if (data == null || data.Length == 0) return;

        AudioClip clip = AudioClip.Create("VoiceClip", data.Length, 1, 16000, false);
        clip.SetData(data, 0);

        Debug.Log($"[VoiceReceiver] Playing received audio clip ({data.Length} samples)");
        audioSource.clip = clip;
        audioSource.Play();
    }
}
