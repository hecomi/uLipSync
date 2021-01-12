using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace uLipSync
{

public class uLipSync2 : MonoBehaviour
{
    public Profile2 profile2;
    public bool calibration = true;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    [Range(0f, 0.1f)] public float minError = 1e-4f;
    [Range(0f, 2f)] public float outputSoundGain = 1f;

    NativeArray<float> rawData_;
    NativeArray<float> inputData_;
    NativeArray<float> lpcSpectral_;
    NativeArray<LipSyncJob2.Result> jobResult_;

    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;

    public LipSyncInfo result { get; private set; } = new LipSyncInfo();

#if UNITY_EDITOR
    NativeArray<float> lpcSpectralForEditorOnly_;
    public NativeArray<float> lpcSpectralEnvelopeForEditorOnly 
    { 
        get { return lpcSpectralForEditorOnly_; } 
    }

    NativeArray<float> fftDataJob_;
    NativeArray<float> fftDataEditor_;
    public NativeArray<float> fftDataEditor
    { 
        get { return fftDataEditor_; } 
    }
#endif

    void OnEnable()
    {
        AllocateBuffers();
    }

    void OnDisable()
    {
        DisposeBuffers();
    }

    void Update()
    {
        if (!jobHandle_.IsCompleted) return;

        GetResult();
        InvokeCallback();
        profile2.RebuildIfNeeded();
        ScheduleJob();

        UpdateBuffers();
        UpdateCalibration();
    }

    void AllocateBuffers()
    {
        lock (lockObject_)
        {
            rawData_ = new NativeArray<float>(profile2.sampleCount, Allocator.Persistent);
            inputData_ = new NativeArray<float>(profile2.sampleCount, Allocator.Persistent); 
            lpcSpectral_ = new NativeArray<float>(profile2.frequencyResolution, Allocator.Persistent); 
            jobResult_ = new NativeArray<LipSyncJob2.Result>(1, Allocator.Persistent);
#if UNITY_EDITOR
            lpcSpectralForEditorOnly_ = new NativeArray<float>(lpcSpectral_.Length, Allocator.Persistent); 
            fftDataJob_ = new NativeArray<float>(profile2.sampleCount, Allocator.Persistent); 
            fftDataEditor_ = new NativeArray<float>(profile2.sampleCount, Allocator.Persistent); 
#endif
        }
    }

    void DisposeBuffers()
    {
        lock (lockObject_)
        {
            jobHandle_.Complete();
            rawData_.Dispose();
            inputData_.Dispose();
            lpcSpectral_.Dispose();
            jobResult_.Dispose();
#if UNITY_EDITOR
            lpcSpectralForEditorOnly_.Dispose();
            fftDataJob_.Dispose();
            fftDataEditor_.Dispose();
#endif
        }
    }

    void UpdateBuffers()
    {
        if (profile2.sampleCount != rawData_.Length ||
            profile2.frequencyResolution != lpcSpectral_.Length)
        {
            lock (lockObject_)
            {
                DisposeBuffers();
                AllocateBuffers();
            }
        }
    }

    void UpdateCalibration()
    {
        if (Input.GetKeyDown(KeyCode.A)) SetSpectralEnvelopeToProfile(Vowel.A);
        if (Input.GetKeyDown(KeyCode.I)) SetSpectralEnvelopeToProfile(Vowel.I);
        if (Input.GetKeyDown(KeyCode.U)) SetSpectralEnvelopeToProfile(Vowel.U);
        if (Input.GetKeyDown(KeyCode.E)) SetSpectralEnvelopeToProfile(Vowel.E);
        if (Input.GetKeyDown(KeyCode.O)) SetSpectralEnvelopeToProfile(Vowel.O);
    }

    void GetResult()
    {
        jobHandle_.Complete();

#if UNITY_EDITOR
        lpcSpectralForEditorOnly_.CopyFrom(lpcSpectral_);
        fftDataEditor_.CopyFrom(fftDataJob_);
#endif

        Debug.Log(jobResult_[0].vowel);
    }

    void InvokeCallback()
    {
        if (onLipSyncUpdate == null) return;
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            inputData_.CopyFrom(rawData_);
            index = index_;
        }

        var lipSyncJob = new LipSyncJob2()
        {
            input = inputData_,
            startIndex = index,
            lpcOrder = profile2.lpcOrder,
            sampleRate = AudioSettings.outputSampleRate,
            maxFreq = profile2.maxFrequency,
            windowFunc = profile2.windowFunc,
            H = lpcSpectral_,
            result = jobResult_,
            volumeThresh = 1e-4f,
            a = profile2.GetNativeArray(Vowel.A),
            i = profile2.GetNativeArray(Vowel.I),
            u = profile2.GetNativeArray(Vowel.U),
            e = profile2.GetNativeArray(Vowel.E),
            o = profile2.GetNativeArray(Vowel.O),
        };

        jobHandle_ = lipSyncJob.Schedule();

#if UNITY_EDITOR
        var fftJob = new FftJob()
        {
            input = inputData_,
            startIndex = index,
            spectrum = fftDataJob_,
            windowFunc = profile2.windowFunc,
            volumeThresh = 1e-4f,
        };

        jobHandle_ = fftJob.Schedule(jobHandle_);
#endif
    }

    [BurstCompile]
	void OnAudioFilterRead(float[] input, int channels)
	{
        if (rawData_ != null)
        {
            lock (lockObject_)
            {
                index_ = index_ % rawData_.Length;
                for (int i = 0; i < input.Length; i += channels) 
                {
                    rawData_[index_++ % rawData_.Length] = input[i];
                }
            }
        }

        if (math.abs(outputSoundGain - 1f) > math.EPSILON)
        {
            for (int i = 0; i < input.Length; ++i) 
            {
                input[i] *= outputSoundGain;
            }
        }
	}

    public void SetSpectralEnvelopeToProfile(Vowel vowel)
    {
        var H = lpcSpectralForEditorOnly_;
        profile2.Set(vowel, H);
    }
}

}
