using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace uLipSync
{

public class uLipSync : MonoBehaviour
{
    public Profile profile;
    public Config config;
    [Range(0f, 2f)] public float outputSoundGain = 1f;
    [Range(0f, 1f)] public float filter = 0.7f;
    [Range(0f, 1f)] public float maxVolume = 0.01f;
    [Range(0f, 1f)] public float minVolume = 0.0001f;
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
    public int sampleCount { get { return profile ? config.sampleCount : 1024; } }

    LipSyncInfo rawResult_ = new LipSyncInfo();
    LipSyncInfo result_ = new LipSyncInfo();
    public LipSyncInfo result 
    { 
        get { return result_; } 
    }

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
    [HideInInspector] public bool foldOutVisualizer = false;
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

        jobHandle_.Complete();
        GetResultAndInvokeCallback();
        ScheduleJob();

        UpdateBuffers();
    }

    void AllocateBuffers()
    {
        rawData_ = new NativeArray<float>(sampleCount, Allocator.Persistent);
        inputData_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        lpcSpectralEnvelope_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        dLpcSpectralEnvelope_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        ddLpcSpectralEnvelope_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        jobResult_ = new NativeArray<LipSyncJob.Result>(2, Allocator.Persistent);
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_ = new NativeArray<float>(lpcSpectralEnvelope_.Length, Allocator.Persistent); 
        ddLpcSpectralEnvelopeForEditorOnly_ = new NativeArray<float>(ddLpcSpectralEnvelope_.Length, Allocator.Persistent); 
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
#endif
    }

    void UpdateBuffers()
    {
        if (sampleCount == rawData_.Length || sampleCount == 0) return;

        lock (lockObject_)
        {
            DisposeBuffers();
            AllocateBuffers();
        }
    }

    void GetResultAndInvokeCallback()
    {
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_.CopyFrom(lpcSpectralEnvelope_);
        ddLpcSpectralEnvelopeForEditorOnly_.CopyFrom(ddLpcSpectralEnvelope_);
#endif

        if (onLipSyncUpdate == null) return;

        var vowelInfo = LipSyncUtil.GetVowel(jobResult_[0].f1, jobResult_[0].f2, jobResult_[0].f3, profile);
        var vowelInfoBySecondDerivative = LipSyncUtil.GetVowel(jobResult_[1].f1, jobResult_[1].f2, jobResult_[1].f3, profile);
        if (vowelInfo.diff < vowelInfoBySecondDerivative.diff)
        {
            UpdateLipSyncInfo(
                jobResult_[0].volume, 
                vowelInfo.formant, 
                vowelInfo.vowel);
        }
        else
        {
            UpdateLipSyncInfo(
                jobResult_[1].volume, 
                vowelInfoBySecondDerivative.formant, 
                vowelInfoBySecondDerivative.vowel);
        }

        onLipSyncUpdate.Invoke(result);
    }

    void UpdateLipSyncInfo(float volume, FormantPair formant, Vowel vowel)
    {
        float a = 1f - filter;

        float normalizedVolume = Mathf.Clamp((volume - minVolume) / (maxVolume - minVolume), 0f, 1f);
        rawResult_.volume = normalizedVolume;
        result_.volume += (normalizedVolume - result_.volume) * a;

        rawResult_.formant = result_.formant = formant;

        if (volume < minVolume) return;

        float max = 0f;
        float sum = 0f;
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var key = (Vowel)i;
            float target = key == vowel ? 1f : 0f;
            float value = rawResult_.vowels[key];
            value += (target - value) * a;
            if (value > max)
            {
                rawResult_.mainVowel = key;
                max = value;
            }
            rawResult_.vowels[key] = value;
            sum += value;
        }

        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var key = (Vowel)i;
            result_.vowels[key] = rawResult_.vowels[key] / sum;
        }
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            inputData_.CopyFrom(rawData_);
            index = index_;
        }

        var job = new LipSyncJob()
        {
            input = inputData_,
            startIndex = index,
            lpcOrder = config.lpcOrder,
            sampleRate = AudioSettings.outputSampleRate,
            H = lpcSpectralEnvelope_,
            dH = dLpcSpectralEnvelope_,
            ddH = ddLpcSpectralEnvelope_,
            result = jobResult_,
            volumeThresh = minVolume,
        };

        jobHandle_ = job.Schedule();
    }

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

        if (Mathf.Abs(outputSoundGain - 1f) > Mathf.Epsilon)
        {
            for (int i = 0; i < input.Length; ++i) 
            {
                input[i] *= outputSoundGain;
            }
        }
	}
}

}
