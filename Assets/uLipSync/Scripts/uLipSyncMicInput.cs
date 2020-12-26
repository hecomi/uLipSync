using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncMicInput : MonoBehaviour
{
    public int micIndex = 0;

    public AudioSource source { get; private set; }
    public bool isReady { get; private set; } = false;
    public bool isRecording { get; private set; } = false;
    MicInfo mic { get; set; } = new MicInfo();
    public int micFreq { get { return mic.minFreq; } }
    public int maxFreq { get { return mic.maxFreq; } }

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
        var mics = MicrophoneUtil.GetList();
        if (mics.Count <= 0) return;

        if (micIndex < 0 || micIndex >= mics.Count) micIndex = 0;

        mic = mics[micIndex];

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
        clip = Microphone.Start(mic.name, true, 10, maxFreq);
        while (Microphone.GetPosition(mic.name) <= 0) ;
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
