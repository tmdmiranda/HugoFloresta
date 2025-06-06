using UnityEngine;

public class MicrophoneRecorder : MonoBehaviour
{
    public AudioClip micClip;
    public int sampleRate = 16000;
    public int micLengthSec = 1;

    void Start()
    {
        micClip = Microphone.Start(null, true, micLengthSec, sampleRate);
    }

    public float[] GetLatestSample()
    {
        if (micClip == null) return null;

        int micPos = Microphone.GetPosition(null);
        if (micPos < 0 || micPos < 1024) return null; // Avoid out-of-range

        float[] sample = new float[1024];
        micClip.GetData(sample, micPos - 1024);
        return sample;
    }

}
