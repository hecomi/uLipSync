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
    NativeArray<CalcFormantsResult> result_;
    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;
#if UNITY_EDITOR
    NativeArray<float> lpcSpectralEnvelopeForEditorOnly_;
    public NativeArray<float> lpcSpectralEnvelopeForEditorOnly 
    { 
        get { return lpcSpectralEnvelopeForEditorOnly_; } 
    }
#endif
    public int sampleCount { get { return config ? config.sampleCount : 1024; } }
    CalcFormantsResult lastResult_ = new CalcFormantsResult();
    public CalcFormantsResult result { get { return lastResult_; } }

    void OnEnable()
    {
        rawData_ = new NativeArray<float>(sampleCount, Allocator.Persistent);
        inputData_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        lpcSpectralEnvelope_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        result_ = new NativeArray<CalcFormantsResult>(1, Allocator.Persistent);
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_ = new NativeArray<float>(lpcSpectralEnvelope_.Length, Allocator.Persistent); 
#endif
    }

    void OnDisable()
    {
        jobHandle_.Complete();
        rawData_.Dispose();
        inputData_.Dispose();
        lpcSpectralEnvelope_.Dispose();
        result_.Dispose();
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_.Dispose();
#endif
    }

    void Update()
    {
        if (!jobHandle_.IsCompleted) return;

        jobHandle_.Complete();
        GetResultAndInvokeCallback();
        ScheduleJob();
    }

    void GetResultAndInvokeCallback()
    {
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditorOnly_.CopyFrom(lpcSpectralEnvelope_);
#endif

        if (onLipSyncUpdate == null) return;

        lastResult_ = result_[0];
        var info = new LipSyncInfo()
        {
            volume = result.volume,
            formant = result.formant,
            vowel = LipSyncUtil.GetVowel(result.formant, config),
        };
        onLipSyncUpdate.Invoke(info);
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            inputData_.CopyFrom(rawData_);
            index = index_;
        }

        var job = new CalcFormantsJob()
        {
            input = inputData_,
            startIndex = index,
            lpcOrder = config.lpcOrder,
            sampleRate = AudioSettings.outputSampleRate,
            H = lpcSpectralEnvelope_,
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
