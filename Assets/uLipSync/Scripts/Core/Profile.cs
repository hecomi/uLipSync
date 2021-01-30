using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Burst;
using System.Collections.Generic;

namespace uLipSync
{

[System.Serializable]
public struct Mfcc
{
    public float[] array;
    public float this[int i] { get { return array[i]; } }
    public int length { get { return array.Length; } }
}

[System.Serializable]
public class MfccData
{
    public List<Mfcc> mfccList = new List<Mfcc>();
    public NativeArray<float> averages;

    [BurstCompile]
    public void Allocate()
    {
        averages = new NativeArray<float>(12, Allocator.Persistent);
    }

    [BurstCompile]
    public void Deallocate()
    {
        if (averages.IsCreated) 
        {
            averages.Dispose();
        }
    }

    public void Add(float[] mfcc)
    {
        mfccList.Add(new Mfcc() { array = mfcc });
    }

    public void RemoveOldData(int dataCount)
    {
        while (mfccList.Count > dataCount) mfccList.RemoveAt(0);
    }

    [BurstCompile]
    public void Update()
    {
        if (mfccList.Count == 0) return;

        for (int i = 0; i < averages.Length; ++i)
        {
            averages[i] = 0;
        }

        for (int i = 0; i < 12; ++i)
        {
            averages[i] = 0f;
            foreach (var mfcc in mfccList)
            {
                Assert.AreEqual(mfcc.length, 12);
                averages[i] += mfcc[i];
            }
            averages[i] /= mfccList.Count;
        }
    }

    public float GetAverage(int i)
    {
        return averages[i];
    }
}

[CreateAssetMenu(menuName = Common.assetName + "/Profile"), BurstCompile]
public class Profile : ScriptableObject
{
    public int mfccDataCount = 32;
    public int melFilterBankChannels = 24;
    public int targetSampleRate = 16000;
    public int sampleCount = 512;
    [Range(-10f, 0f)] public float minVolume = -4f;
    [Range(-10f, 0f)] public float maxVolume = -2f;
    [Range(0f, 50f)] public float maxError = 30f;

    public MfccData a = new MfccData();
    public MfccData i = new MfccData();
    public MfccData u = new MfccData();
    public MfccData e = new MfccData();
    public MfccData o = new MfccData();

    MfccData Get(Vowel vowel)
    {
        switch (vowel)
        {
            case Vowel.A: return a;
            case Vowel.I: return i;
            case Vowel.U: return u;
            case Vowel.E: return e;
            case Vowel.O: return o;
            default: return null;
        }
    }

    void OnEnable()
    {
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var data = Get((Vowel)i);
            data.Allocate();
            data.RemoveOldData(mfccDataCount);
            data.Update();
        }
    }

    void OnDisable()
    {
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            Get((Vowel)i).Deallocate();
        }
    }

    [BurstCompile]
    public void Add(Vowel vowel, NativeArray<float> mfcc, bool calib)
    {
        var array = new float[mfcc.Length];
        mfcc.CopyTo(array);
        var data = Get(vowel);
        data.Add(array);
        data.RemoveOldData(mfccDataCount);
        if (calib) data.Update();
    }

    public NativeArray<float> GetAverages(Vowel vowel)
    {
        return Get(vowel).averages;
    }
}

}
