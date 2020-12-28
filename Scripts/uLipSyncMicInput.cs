using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncMicInput : MonoBehaviour
{
    public int index = 0;
    private int preIndex_ = 0;
    public bool isAutoStart = false;

    public AudioSource source { get; private set; }
    public bool isReady { get; private set; } = false;
    public bool isRecording { get; private set; } = false;
    public MicDevice device { get; private set; } = new MicDevice();
    public int micFreq { get { return device.minFreq; } }
    public int maxFreq { get { return device.maxFreq; } }

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

    protected void OnEnable()
    {
        source = GetComponent<AudioSource>();

        preIndex_ = index;

        UpdateMicInfo();

        if (isAutoStart)
        {
            StartRecord();
        }
    }

    void OnDisable()
    {
        StopRecordInternal();
    }

    void OnApplicationPause()
    {
        StopRecordInternal();
        source.Stop();
        Destroy(clip);
    }

    void Update()
    {
        UpdateDevice();

        if (!isPlaying && isReady && isRecording)
        {
            StartRecordInternal();
        }

        if (isPlaying && !isRecording)
        {
            StopRecordInternal();
        }
    }

    public void UpdateMicInfo()
    {
        var mics = MicUtil.GetDeviceList();
        if (mics.Count <= 0) return;

        if (index < 0 || index >= mics.Count) index = 0;

        device = mics[index];

        isReady = true;
    }

    void UpdateDevice()
    {
        if (preIndex_ != index)
        {
            preIndex_ = index;
            StopRecordInternal();
            UpdateMicInfo();
        }
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
        clip = Microphone.Start(device.name, true, 10, maxFreq);
        while (Microphone.GetPosition(device.name) <= 0) ;
        source.loop = true;
        source.Play();
    }

    void StopRecordInternal()
    {
        source.Stop();
        DestroyImmediate(clip);
    }
}

}
