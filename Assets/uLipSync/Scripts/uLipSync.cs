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
    public bool calibration = true;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    [Range(0f, 2f)] public float outputSoundGain = 1f;

    NativeArray<float> rawData_;
    NativeArray<float> inputData_;
    NativeArray<float> mfcc_;
    NativeArray<float> mfccForOther_;
    NativeArray<LipSyncJob.Result> jobResult_;

    public NativeArray<float> mfcc { get { return mfccForOther_; } }
    public bool isMfccUpdated { get; set; } = false;

    int sampleCount
    {
        get 
        {  
            float r = (float)AudioSettings.outputSampleRate / profile.targetSampleRate;
            return Mathf.CeilToInt(Common.sampleCount * r);
        }
    }

    JobHandle jobHandle_;
    object lockObject_ = new object();
    int index_ = 0;

    public LipSyncInfo result { get; private set; } = new LipSyncInfo();

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
        isMfccUpdated = false;

        if (!jobHandle_.IsCompleted) return;

        GetResult();
        InvokeCallback();
        ScheduleJob();

        UpdateBuffers();
        UpdateCalibration();
    }

    void AllocateBuffers()
    {
        lock (lockObject_)
        {
            int n = sampleCount;
            rawData_ = new NativeArray<float>(n, Allocator.Persistent);
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
            rawData_.Dispose();
            inputData_.Dispose();
            mfcc_.Dispose();
            mfccForOther_.Dispose();
            jobResult_.Dispose();
        }
    }

    void UpdateBuffers()
    {
        if (sampleCount != rawData_.Length)
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
        if (Input.GetKey(KeyCode.A)) AddMfccToProfile(Vowel.A);
        if (Input.GetKey(KeyCode.I)) AddMfccToProfile(Vowel.I);
        if (Input.GetKey(KeyCode.U)) AddMfccToProfile(Vowel.U);
        if (Input.GetKey(KeyCode.E)) AddMfccToProfile(Vowel.E);
        if (Input.GetKey(KeyCode.O)) AddMfccToProfile(Vowel.O);
    }

    void GetResult()
    {
        jobHandle_.Complete();
        mfccForOther_.CopyFrom(mfcc_);
        isMfccUpdated = true;

        if (jobResult_[0].volume > 0.001f)
        {
            Debug.Log(jobResult_[0].vowel + "  " + jobResult_[0].distance);
        }
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

        var lipSyncJob = new LipSyncJob()
        {
            input = inputData_,
            startIndex = index,
            outputSampleRate = AudioSettings.outputSampleRate,
            targetSampleRate = profile.targetSampleRate,
            volumeThresh = 1e-4f,
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

    public void AddMfccToProfile(Vowel vowel)
    {
        profile.Add(vowel, mfcc);
    }
}

}
