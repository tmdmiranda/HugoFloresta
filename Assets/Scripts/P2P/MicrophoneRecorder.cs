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
        int micPos = Microphone.GetPosition(null);
        float[] sample = new float[1024];
        micClip.GetData(sample, micPos - sample.Length);
        return sample;
    }
}
