using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace uLipSync
{

public class LipSync : MonoBehaviour
{
    public Config config;
    public bool muteInputSound = false;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    NativeArray<float> rawData_;
    NativeArray<float> inputData_;
    NativeArray<float> lpcSpectralEnvelope_;
    NativeArray<CalcFormantsResult> result_;
    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;
    int sampleRate_ = 48000;
#if UNITY_EDITOR
    NativeArray<float> lpcSpectralEnvelopeForEditor_;
    public NativeArray<float> lpcSpectralEnvelopeForEditor { get { return lpcSpectralEnvelopeForEditor_; } }
#endif

    public int sampleCount { get { return config ? config.sampleCount : 1024; } }
    public float deltaFreq { get { return (float)sampleRate_ / sampleCount; } }

    CalcFormantsResult resultA_ = new CalcFormantsResult();
    public CalcFormantsResult result { get { return resultA_; } }

    void OnEnable()
    {
        sampleRate_ = AudioSettings.outputSampleRate;
        rawData_ = new NativeArray<float>(sampleCount, Allocator.Persistent);
        inputData_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        lpcSpectralEnvelope_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        result_ = new NativeArray<CalcFormantsResult>(1, Allocator.Persistent);
#if UNITY_EDITOR
        lpcSpectralEnvelopeForEditor_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
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
        lpcSpectralEnvelopeForEditor_.Dispose();
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
        lpcSpectralEnvelopeForEditor_.CopyFrom(lpcSpectralEnvelope_);
#endif

        if (onLipSyncUpdate == null) return;

        resultA_ = result_[0];
        var info = new LipSyncInfo()
        {
            volume = result.volume,
            formant = result.formant,
            vowel = Util.GetVowel(result.formant, config),
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
            deltaFreq = deltaFreq,
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
                int n = input.Length;
                for (int i = 0; i < n; i += channels) 
                {
                    rawData_[index_] = input[i];
                    index_ = (index_ + 1) % n;
                }
            }
        }

        if (muteInputSound)
        {
            System.Array.Clear(input, 0, input.Length);   
        }
	}
}

}
