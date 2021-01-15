using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace uLipSync
{

[System.Serializable]
public struct MfccArray
{
    public float[] array;
}

[System.Serializable]
public class MfccData
{
    // public string phenome;
    public List<MfccArray> mfccList = new List<MfccArray>();
    public NativeArray<float2> averageAndVariance;

    [BurstCompile]
    public void Allocate()
    {
        averageAndVariance = new NativeArray<float2>(12, Allocator.Persistent);
    }

    [BurstCompile]
    public void Deallocate()
    {
        if (averageAndVariance.IsCreated) 
        {
            averageAndVariance.Dispose();
        }
    }

    public void Add(float[] mfcc)
    {
        mfccList.Add(new MfccArray() { array = mfcc });
        while (mfccList.Count > 10) mfccList.RemoveAt(0);
    }

    [BurstCompile]
    public void Update()
    {
        if (mfccList.Count == 0) return;

        for (int i = 0; i < 12; ++i)
        {
            float m = 0f;
            float s = 0f;
            foreach (var mfcc in mfccList)
            {
                Assert.AreEqual(mfcc.array.Length, 12);
                float x = mfcc.array[i];
                m += x;
                s += x * x;
            }
            m /= mfccList.Count;
            s /= mfccList.Count;
            s -= m * m;
            averageAndVariance[i] = new float2(m, s);
        }
    }
}

[CreateAssetMenu(menuName = Common.assetName + "/Profile"), BurstCompile]
public class Profile : ScriptableObject
{
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
    public void Add(Vowel vowel, NativeArray<float> mfcc)
    {
        var array = new float[mfcc.Length];
        mfcc.CopyTo(array);
        Get(vowel).Add(array);
    }

    public NativeArray<float2> GetAverageAndVarianceOfMfcc(Vowel vowel)
    {
        return Get(vowel).averageAndVariance;
    }
}

}
