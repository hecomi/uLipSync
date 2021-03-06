using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncAudioSource : MonoBehaviour
{
    public AudioFilterReadEvent onAudioFilterRead { get; private set; } = new AudioFilterReadEvent();

    void OnAudioFilterRead(float[] input, int channels)
    {
        if (onAudioFilterRead != null) 
        {
            onAudioFilterRead.Invoke(input, channels);
        }
    }
}

}
