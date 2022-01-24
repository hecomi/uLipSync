using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncMicrophone : MonoBehaviour
{
    const int maxRetryMilliSec = 1000;

    public int index = 0;
    private int preIndex_ = 0;
    public bool isAutoStart = true;

    public AudioSource source { get; private set; }
    public bool isReady { get; private set; } = false;
    public bool isStartRequested { get; private set; } = false;
    public bool isStopRequested { get; private set; } = false;
    public bool isRecording { get; private set; } = false;
    public MicDevice device { get; private set; } = new MicDevice();
    public int micFreq { get { return device.minFreq; } }
    public int maxFreq { get { return device.maxFreq; } }

    public AudioClip clip
    {
        get { return source ? source.clip : null; }
        set { if (source) source.clip = value; }
    }

    public bool isPlaying
    {
        get { return source ? source.isPlaying : false; }
    }

    public float freq
    {
        get { return clip ? clip.frequency : 44100; }
    }

    protected void OnEnable()
    {
        source = GetComponent<AudioSource>();

        preIndex_ = index;

        UpdateMicInfo();

        if (isAutoStart && isReady)
        {
            StartRecord();
        }
    }

    void OnDisable()
    {
        StopRecordInternal();
    }

    void Update()
    {
        UpdateDevice();

        if (isStartRequested)
        {
            isStartRequested = false;
            StartRecordInternal();
        }

        if (isStopRequested)
        {
            isStopRequested = false;
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
        if (preIndex_ == index) return;

        preIndex_ = index;
        StopRecordInternal();
        UpdateMicInfo();
    }

    public void StartRecord()
    {
        if (!isReady)
        {
            Debug.LogError("Microphone has not been initialized yet!");
            return;
        }
        isStartRequested = true;
        isStopRequested = false;
    }

    public void StopRecord()
    {
        isStopRequested = true;
        isStartRequested = false;
    }

    void StartRecordInternal()
    {
        if (!source) return;

        int freq = maxFreq;
        if (freq <= 0) freq = 48000;

        clip = Microphone.Start(device.name, true, 10, freq);

        int retryCount = 0;
        while (Microphone.GetPosition(device.name) <= 0)
        {
            if (++retryCount >= maxRetryMilliSec)
            {
                Debug.LogError("Failed to get microphone.");
                return;
            }
            System.Threading.Thread.Sleep(1);
        }

        source.loop = true;
        source.Play();

        isRecording = true;
    }

    void StopRecordInternal()
    {
        if (!source) return;

        if (source.isPlaying)
        {
            source.Stop();
        }

        isRecording = false;
    }

    public void StopRecordAndCreateAudioClip()
    {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        var newClip = AudioClip.Create("Recorded Data", clip.samples, clip.channels, clip.frequency, false);
        newClip.SetData(data, 0);

        StopRecordInternal();

        clip = newClip;
        source.loop = true;
        source.Play();
    }
}

}
