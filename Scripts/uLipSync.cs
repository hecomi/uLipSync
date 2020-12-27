using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace uLipSync
{

public class uLipSync : MonoBehaviour
{
    public Config config;
    [Range(0f, 2f)] public float outputSoundGain = 1f;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    NativeArray<float> rawData_;
    NativeArray<float> inputData_;
    NativeArray<float> lpcSpectralEnvelope_;
    NativeArray<float> dLpcSpectralEnvelope_;
    NativeArray<float> ddLpcSpectralEnvelope_;
    NativeArray<LipSyncJob.Result> result_;

    JobHandle jobHandle_;
    object lockObject_ = new object();

    int index_ = 0;
    public int sampleCount { get { return config ? config.sampleCount : 1024; } }

    LipSyncInfo lastResult_ = new LipSyncInfo();
    public LipSyncInfo result 
    { 
        get { return lastResult_; } 
        private set { lastResult_ = value; }
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
        result_ = new NativeArray<LipSyncJob.Result>(2, Allocator.Persistent);
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
        result_.Dispose();
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

        var vowelInfo = LipSyncUtil.GetVowel(result_[0].f1, result_[0].f2, result_[0].f3, config);
        var vowelInfoBySecondDerivative = LipSyncUtil.GetVowel(result_[1].f1, result_[1].f2, result_[1].f3, config);
        if (vowelInfo.diff < vowelInfoBySecondDerivative.diff)
        {
            result = new LipSyncInfo()
            {
                volume = result_[0].volume,
                formant = vowelInfo.formant,
                vowel = vowelInfo.vowel,
            };
        }
        else
        {
            result = new LipSyncInfo()
            {
                volume = result_[1].volume,
                formant = vowelInfoBySecondDerivative.formant,
                vowel = vowelInfoBySecondDerivative.vowel,
            };
        }

        onLipSyncUpdate.Invoke(result);
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
            result = result_,
            volumeThresh = config.volumeThresh,
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
