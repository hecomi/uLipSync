using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class MicHandler : MonoBehaviour
{
    public int sampleCount = 1024;
    public int micIndex = 0;

    AudioSource source_;
    int minFreq_;
    int maxFreq_;
    string micName_ = null;

    public bool isReady { get; set; } = false;
    public bool isRecording { get; set; } = false;

    bool isSourcePlainyg 
    { 
        get { return source_ && source_.isPlaying; } 
    }

    public float deltaFreq
    {
        get { return clip ? clip.frequency / sampleCount : 0.0f; }
    }

    public AudioClip clip
    {
        get { return source_.clip; }
        set { source_.clip = value; }
    }

    void OnEnable()
    {
        InitMicInfo();
        source_ = GetComponent<AudioSource>();
        StartRecord();
    }

    void OnDisable()
    {
        StopRecordInternal();
    }

    void Update()
    {
        if (!isSourcePlainyg && isReady && isRecording)
        {
            StartRecordInternal();
        }

        if (isSourcePlainyg && !isRecording)
        {
            StopRecordInternal();
        }
    }

    void OnApplicationPause()
    {
        StopRecordInternal();
        source_.Stop();
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
            micName_ = Microphone.devices[micIndex];
        }

        Microphone.GetDeviceCaps(micName_, out minFreq_, out maxFreq_);
        if (minFreq_ == 0 && maxFreq_ == 0)
        {
            maxFreq_ = 44100;
        }
        else if (maxFreq_ > 44100)
        {
            maxFreq_ = 44100;
        }

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
        clip = Microphone.Start(micName_, false, 10, maxFreq_);
        while (Microphone.GetPosition(micName_) <= 0) ;
        source_.Play();
    }

    void StopRecordInternal()
    {
        source_.Stop();
        Destroy(clip);
    }
}

}
