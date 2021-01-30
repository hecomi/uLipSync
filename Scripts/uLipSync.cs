using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSync : MonoBehaviour
{
    public Profile profile;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    [Tooltip("If you want to supress the sound output, set this value to zero instead of setting the AudioSource volume to zero")]
    [Range(0f, 1f)] public float outputSoundGain = 1f;

    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;

    NativeArray<float> rawInputData_;
    NativeArray<float> inputData_;
    NativeArray<float> mfcc_;
    NativeArray<float> mfccForOther_;
    NativeArray<LipSyncJob.Result> jobResult_;
    List<Vowel> requestedCalibrationVowels_ = new List<Vowel>();

    public NativeArray<float> mfcc { get { return mfccForOther_; } }
    public LipSyncInfo result { get; private set; } = new LipSyncInfo();

    int inputSampleCount
    {
        get 
        {  
            float r = (float)AudioSettings.outputSampleRate / profile.targetSampleRate;
            return Mathf.CeilToInt(profile.sampleCount * r);
        }
    }

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
        UpdateCalibration();
        ScheduleJob();

        UpdateBuffers();
    }

    void AllocateBuffers()
    {
        lock (lockObject_)
        {
            int n = inputSampleCount;
            rawInputData_ = new NativeArray<float>(n, Allocator.Persistent);
            inputData_ = new NativeArray<float>(n, Allocator.Persistent); 
            mfcc_ = new NativeArray<float>(12, Allocator.Persistent); 
            jobResult_ = new NativeArray<LipSyncJob.Result>(1, Allocator.Persistent);
            mfccForOther_ = new NativeArray<float>(12, Allocator.Persistent); 
        }
    }

    void DisposeBuffers()
    {
        lock (lockObject_)
        {
            jobHandle_.Complete();
            rawInputData_.Dispose();
            inputData_.Dispose();
            mfcc_.Dispose();
            mfccForOther_.Dispose();
            jobResult_.Dispose();
        }
    }

    void UpdateBuffers()
    {
        if (inputSampleCount != rawInputData_.Length)
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
        mfccForOther_.CopyFrom(mfcc_);

        var vowel = jobResult_[0].vowel;
        float distance = jobResult_[0].distance;
        float vol = Mathf.Log10(jobResult_[0].volume);
        float minVol = profile.minVolume;
        float maxVol = Mathf.Max(profile.maxVolume, minVol + 1e-4f);
        vol = (vol - minVol) / (maxVol - minVol);
        vol = Mathf.Clamp(vol, 0f, 1f);
        if (distance > profile.maxError )
        {
            vol = 0f;
        }

        result = new LipSyncInfo()
        {
            vowel = vowel,
            volume = vol,
            rawVolume = jobResult_[0].volume,
            distance = distance,
        };
    }

    void InvokeCallback()
    {
        if (onLipSyncUpdate == null) return;

        onLipSyncUpdate.Invoke(result);
    }

    void ScheduleJob()
    {
        int index = 0;
        lock (lockObject_)
        {
            inputData_.CopyFrom(rawInputData_);
            index = index_;
        }

        var lipSyncJob = new LipSyncJob()
        {
            input = inputData_,
            startIndex = index,
            outputSampleRate = AudioSettings.outputSampleRate,
            targetSampleRate = profile.targetSampleRate,
            volumeThresh = Mathf.Pow(10f, profile.minVolume),
            melFilterBankChannels = profile.melFilterBankChannels,
            mfcc = mfcc_,
            a = profile.GetAverages(Vowel.A),
            i = profile.GetAverages(Vowel.I),
            u = profile.GetAverages(Vowel.U),
            e = profile.GetAverages(Vowel.E),
            o = profile.GetAverages(Vowel.O),
            result = jobResult_,
        };

        jobHandle_ = lipSyncJob.Schedule();
    }

    void OnAudioFilterRead(float[] input, int channels)
    {
        if (rawInputData_ == null) return;

        lock (lockObject_)
        {
            index_ = index_ % rawInputData_.Length;
            for (int i = 0; i < input.Length; i += channels) 
            {
                rawInputData_[index_++ % rawInputData_.Length] = input[i];
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

    public void RequestCalibration(Vowel vowel)
    {
        requestedCalibrationVowels_.Add(vowel);
    }

    void UpdateCalibration()
    {
        if (profile == null) return;

        foreach (var vowel in requestedCalibrationVowels_)
        {
            profile.Add(vowel, mfcc, true);
        }
        requestedCalibrationVowels_.Clear();
    }
}

}
