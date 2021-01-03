using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace uLipSync
{

public class uLipSync : MonoBehaviour
{
    public Profile profile;
    public Config config;
    [Range(0f, 2f)] public float outputSoundGain = 1f;
    [Range(0f, 1f)] public float openSmoothness = 0.75f;
    [Range(0f, 1f)] public float closeSmoothness = 0.9f;
    [Range(0f, 1f)] public float vowelTransitionSmoothness = 0.8f;
    [Range(0f, 1f)] public float maxVolume = 0.01f;
    [Range(0f, 1f)] public float minVolume = 0.0001f;
    public bool autoVolume = true;
    [Range(0f, 1f)] public float autoVolumeAmp = 0.2f;
    [Range(0f, 1f)] public float autoVolumeFilter = 0.9f;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    NativeArray<float> rawData_;
    NativeArray<float> inputData_;
    NativeArray<float> lpcSpectralEnvelope_;
    NativeArray<float> dLpcSpectralEnvelope_;
    NativeArray<float> ddLpcSpectralEnvelope_;
    NativeArray<LipSyncJob.Result> jobResult_;

    JobHandle jobHandle_;
    object lockObject_ = new object();

    int index_ = 0;

    public int lpcOrder { get { return config ? config.lpcOrder : 64; } }
    public int sampleCount { get { return config ? config.sampleCount : 1024; } }
    public int maxFreq { get { return config ? config.maxFrequency : 3000; } }
    public int freqResolution { get { return config ? config.frequencyResolution : 256; } }

    LipSyncInfo rawResult_ = new LipSyncInfo();
    public LipSyncInfo result { get; private set; } = new LipSyncInfo();

#if UNITY_EDITOR
    NativeArray<float> lpcSpectralEnvelopeForEditorOnly_;
    public NativeArray<float> lpcSpectralEnvelopeForEditorOnly 
    { 
        get { return lpcSpectralEnvelopeForEditorOnly_; } 
    }

    NativeArray<float> ddLpcSpectralEnvelopeForEditorOnly_;
    public NativeArray<float> ddLpcSpectralEnvelopeForEditorOnly 
    { 
        get { return ddLpcSpectralEnvelopeForEditorOnly_; } 
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
        InitAutoVolume();
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
        ScheduleJob();

        UpdateBuffers();
        UpdateAutoVolume();
    }

    void AllocateBuffers()
    {
        rawData_ = new NativeArray<float>(sampleCount, Allocator.Persistent);
        inputData_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        lpcSpectralEnvelope_ = new NativeArray<float>(freqResolution, Allocator.Persistent); 
        dLpcSpectralEnvelope_ = new NativeArray<float>(freqResolution, Allocator.Persistent); 
        ddLpcSpectralEnvelope_ = new NativeArray<float>(freqResolution, Allocator.Persistent); 
        jobResult_ = new NativeArray<LipSyncJob.Result>(2, Allocator.Persistent);
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_ = new NativeArray<float>(lpcSpectralEnvelope_.Length, Allocator.Persistent); 
        ddLpcSpectralEnvelopeForEditorOnly_ = new NativeArray<float>(ddLpcSpectralEnvelope_.Length, Allocator.Persistent); 
        fftDataJob_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        fftDataEditor_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
#endif
    }

    void DisposeBuffers()
    {
        jobHandle_.Complete();
        rawData_.Dispose();
        inputData_.Dispose();
        lpcSpectralEnvelope_.Dispose();
        dLpcSpectralEnvelope_.Dispose();
        ddLpcSpectralEnvelope_.Dispose();
        jobResult_.Dispose();
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_.Dispose();
        ddLpcSpectralEnvelopeForEditorOnly_.Dispose();
        fftDataJob_.Dispose();
        fftDataEditor_.Dispose();
#endif
    }

    void UpdateBuffers()
    {
        if (sampleCount != rawData_.Length ||
            freqResolution != lpcSpectralEnvelope_.Length)
        {
            lock (lockObject_)
            {
                DisposeBuffers();
                AllocateBuffers();
            }
        }
    }

    void GetResult()
    {
        jobHandle_.Complete();

#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_.CopyFrom(lpcSpectralEnvelope_);
        ddLpcSpectralEnvelopeForEditorOnly_.CopyFrom(ddLpcSpectralEnvelope_);
        fftDataEditor_.CopyFrom(fftDataJob_);
#endif
    }

    void InvokeCallback()
    {
        if (onLipSyncUpdate == null) return;

        var vowelInfo = config.checkThirdFormant ?
            LipSyncUtil.GetVowel(jobResult_[0].f1, jobResult_[0].f2, jobResult_[0].f3, profile) :
            LipSyncUtil.GetVowel(new FormantPair(jobResult_[0].f1, jobResult_[0].f2), profile);

        float volume = jobResult_[0].volume;
        FormantPair formant = vowelInfo.formant;
        Vowel vowel = vowelInfo.vowel;

        if (config.checkSecondDerivative)
        {
            var vowelInfoBySecondDerivative = config.checkThirdFormant ?
                LipSyncUtil.GetVowel(jobResult_[1].f1, jobResult_[1].f2, jobResult_[1].f3, profile) :
                LipSyncUtil.GetVowel(new FormantPair(jobResult_[1].f1, jobResult_[1].f2), profile);
            if (vowelInfo.diff > vowelInfoBySecondDerivative.diff)
            {
                formant = vowelInfoBySecondDerivative.formant;
                vowel = vowelInfoBySecondDerivative.vowel;
            }
        }

        UpdateLipSyncInfo(volume, formant, vowel);

        onLipSyncUpdate.Invoke(result);
    }

    void UpdateLipSyncInfo(float volume, FormantPair formant, Vowel vowel)
    {
        float sf = 1f - openSmoothness;
        float sb = 1f - closeSmoothness;
        float preVolume = result.volume;

        rawResult_.volume = volume;

        float normalizedVolume = Mathf.Clamp((volume - minVolume) / (maxVolume - minVolume), 0f, 1f);
        float smooth = normalizedVolume > preVolume ? sf : sb;
        result.volume += (normalizedVolume - preVolume) * smooth;

        rawResult_.formant = result.formant = formant;

        if (volume < minVolume) return;

        if (vowel == Vowel.None) return;

        float max = 0f;
        float sum = 0f;
        for (int i = (int)Vowel.A; i <= (int)Vowel.None; ++i)
        {
            var key = (Vowel)i;
            float target = key == vowel ? 1f : 0f;
            float value = rawResult_.vowels[key];
            value += (target - value) * (1f - vowelTransitionSmoothness);
            if (value > max)
            {
                rawResult_.mainVowel = key;
                max = value;
            }
            rawResult_.vowels[key] = value;
            sum += value;
        }

        result.mainVowel = rawResult_.mainVowel;

        for (int i = (int)Vowel.A; i <= (int)Vowel.None; ++i)
        {
            var key = (Vowel)i;
            if (sum > Mathf.Epsilon)
            {
                result.vowels[key] = rawResult_.vowels[key] / sum;
            }
            else
            {
                result.vowels[key] = 0f;
            }
        }
    }

    void InitAutoVolume()
    {
        if (!autoVolume) return;
        
        maxVolume = minVolume + 0.001f;
    }

    void UpdateAutoVolume()
    {
        if (!autoVolume) return;

        maxVolume *= autoVolumeFilter;
        maxVolume = Mathf.Max(maxVolume, rawResult_.volume * autoVolumeAmp);
        maxVolume = Mathf.Max(maxVolume, minVolume + 0.001f);
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            inputData_.CopyFrom(rawData_);
            index = index_;
        }

        var lipSyncJob = new LipSyncJob()
        {
            input = inputData_,
            startIndex = index,
            lpcOrder = lpcOrder,
            sampleRate = AudioSettings.outputSampleRate,
            maxFreq = maxFreq,
            windowFunc = config.windowFunc,
            H = lpcSpectralEnvelope_,
            dH = dLpcSpectralEnvelope_,
            ddH = ddLpcSpectralEnvelope_,
            result = jobResult_,
            volumeThresh = minVolume,
            minLog10H = profile.minLog10H,
            filterH = config.filterH,
        };

        jobHandle_ = lipSyncJob.Schedule();

#if UNITY_EDITOR
        var fftJob = new FftJob()
        {
            input = inputData_,
            startIndex = index,
            spectrum = fftDataJob_,
            volumeThresh = minVolume,
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
                int n = rawData_.Length;
                index_ = index_ % n;
                for (int i = 0; i < input.Length; i += channels) 
                {
                    rawData_[index_] = input[i];
                    index_ = (index_ + 1) % n;
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
}

}
