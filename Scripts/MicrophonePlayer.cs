using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class MicrophonePlayer : MonoBehaviour
{
    public int micIndex = 0;

    public AudioSource source { get; private set; }
    public bool isReady { get; private set; } = false;
    public bool isRecording { get; private set; } = false;
    public string micName { get; private set; } = "";

    int minFreq_;
    int maxFreq_;
    public int micFreq { get { return minFreq_; } }
    public int maxFreq { get { return maxFreq_; } }

    bool isPlaying 
    { 
        get { return source && source.isPlaying; } 
    }

    public AudioClip clip
    {
        get { return source ? source.clip : null; }
        set { if (source) source.clip = value; }
    }

    public float freq
    {
        get {  return clip ? clip.frequency : 44100; }
    }

    void OnEnable()
    {
        InitMicInfo();
        source = GetComponent<AudioSource>();

        // TODO: start manually
        StartRecord();
    }

    void OnDisable()
    {
        StopRecordInternal();
    }

    void Update()
    {
        if (!isPlaying && isReady && isRecording)
        {
            StartRecordInternal();
        }

        if (isPlaying && !isRecording)
        {
            StopRecordInternal();
        }
    }

    void OnApplicationPause()
    {
        StopRecordInternal();
        source.Stop();
        Destroy(clip);
    }

    void InitMicInfo()
    {
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("Microphone is not connected!");
            return;
        }
        else
        {
            int maxIndex = Microphone.devices.Length - 1;
            if (micIndex > maxIndex)
            {
                micIndex = maxIndex;
            }
            micName = Microphone.devices[micIndex];
        }

        Microphone.GetDeviceCaps(micName, out minFreq_, out maxFreq_);

        isReady = true;
    }

    public void StartRecord()
    {
        if (!isReady)
        {
            Debug.LogError("Mic has not been initialized yet!");
            return;
        }
        isRecording = true;
    }

    public void StopRecord()
    {
        isRecording = false;
    }

    void StartRecordInternal()
    {
        clip = Microphone.Start(micName, true, 10, maxFreq);
        while (Microphone.GetPosition(micName) <= 0) ;
        source.loop = true;
        source.Play();
    }

    void StopRecordInternal()
    {
        source.Stop();
        Destroy(clip);
    }
}

}
