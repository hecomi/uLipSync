using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncMicrophone : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    const int MaxRetryMilliSec = 1000;

    public int index = 0;
    private int _preIndex = 0;
    private AudioClip _micClip = null;

    [Tooltip("When ON, AudioClip of the Microphone input is automatically registered to AudioSource")]
    public bool isAutoStart = true;

    [Range(0.01f, 0.2f)]
    [Tooltip("Threshold time to resynchronize Microphone and AudioSource")]
    public float latencyTolerance = 0.05f;

    [Range(0.01f, 0.2f)]
    [Tooltip("Buffer time for reflecting Microphone input as AudioSource (too low can cause noise)")]
    public float bufferTime = 0.03f;

    public AudioSource source { get; private set; }
    public AudioClip clip
    {
        get => source ? source.clip : null;
        private set { if (source) source.clip = value; }
    }

    public bool isReady { get; private set; } = false;
    public bool isStartRequested { get; private set; } = false;
    public bool isStopRequested { get; private set; } = false;
    public bool isRecording { get; private set; } = false;
    public MicDevice device { get; private set; } = new MicDevice();
    public float latency { get; private set; } = 0f;
    public int micFreq => device.minFreq;
    public int maxFreq => device.maxFreq;
    public bool isMicClipSet => _micClip && clip == _micClip;
    public bool isPlaying => source && source.isPlaying;
    public float freq => clip ? clip.frequency : 44100;
    public bool isOutOfSync => Mathf.Abs(latency) > latencyTolerance + bufferTime;

    protected void OnEnable()
    {
        source = GetComponent<AudioSource>();

        _preIndex = index;

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
        UpdateAudioClip();

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
        
        UpdateLatencyCheck();
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
        var isRecordingNow = isRecording;

        if (_preIndex == index) return;

        _preIndex = index;
        StopRecordInternal();
        UpdateMicInfo();

        if (isRecordingNow)
        {
            StartRecord();
        }
    }

    void UpdateAudioClip()
    {
        if (!isRecording) return;
        if (isMicClipSet) return;

        StopRecordInternal();
        _micClip = null;
    }

    void UpdateLatencyCheck()
    {
        if (!isRecording) return; 

        float micTime = Microphone.GetPosition(device.name) / freq;
        float clipTime = source.time;
        latency = micTime - clipTime;
        
        if (latency < -clip.length / 2) 
        {
            latency += clip.length; 
        }

        if (isOutOfSync)
        {
            if (Microphone.IsRecording(device.name))
            {
                source.time = micTime - bufferTime;
            }
            else
            {
                StartRecord(); 
            }
        }
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

        _micClip = Microphone.Start(device.name, true, 10, freq);
        clip = _micClip;

        int retryCount = 0;
        while (Microphone.GetPosition(device.name) <= 0)
        {
            if (++retryCount >= MaxRetryMilliSec)
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

        if (source.isPlaying && isMicClipSet)
        {
            source.Stop();
        }

        Microphone.End(device.name);

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
#endif
}

}
