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

    NativeArray<float> data_;
    NativeArray<float> input_;
    NativeArray<float> H_;
    NativeArray<CalcFormantsJob.Result> result_;
    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;
    int sampleRate_ = 48000;
#if UNITY_EDITOR
    NativeArray<float> editorOnlyHForDebug_;
    public NativeArray<float> editorOnlyHForDebug { get { return editorOnlyHForDebug_; } }
#endif

    public int sampleCount { get { return config ? config.sampleCount : 1024; } }
    public float deltaFreq { get { return (float)sampleRate_ / sampleCount; } }

    void OnEnable()
    {
        sampleRate_ = AudioSettings.outputSampleRate;
        data_ = new NativeArray<float>(sampleCount, Allocator.Persistent);
        input_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        H_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
        result_ = new NativeArray<CalcFormantsJob.Result>(1, Allocator.Persistent);
#if UNITY_EDITOR
        editorOnlyHForDebug_ = new NativeArray<float>(sampleCount, Allocator.Persistent); 
#endif
    }

    void OnDisable()
    {
        jobHandle_.Complete();
        data_.Dispose();
        input_.Dispose();
        H_.Dispose();
        result_.Dispose();
#if UNITY_EDITOR
        editorOnlyHForDebug_.Dispose();
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
        editorOnlyHForDebug_.CopyFrom(H_);
#endif

        if (onLipSyncUpdate == null) return;

        var volume = result_[0].volume;
        var formant = result_[0].formant;
        var info = new LipSyncInfo()
        {
            volume = volume,
            formant = formant,
            vowel = Util.GetVowel(formant, config),
        };
        onLipSyncUpdate.Invoke(info);
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            input_.CopyFrom(data_);
            index = index_;
        }

        var job = new CalcFormantsJob()
        {
            input = input_,
            startIndex = index,
            lpcOrder = config.lpcOrder,
            deltaFreq = deltaFreq,
            H = H_,
            result = result_,
        };

        jobHandle_ = job.Schedule();
    }

	void OnAudioFilterRead(float[] input, int channels)
	{
        lock (lockObject_)
        {
            int n = data_.Length;
            for (int i = 0; i < n; i += channels) 
            {
                data_[index_] = input[i];
                index_ = (index_ + 1) % n;
            }
        }

        if (muteInputSound)
        {
            System.Array.Clear(input, 0, input.Length);   
        }
	}
}

}
