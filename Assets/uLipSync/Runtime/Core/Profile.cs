using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uLipSync
{

[System.Serializable]
public struct MfccCalibrationData
{
    public float[] array;
    public float this[int i] { get { return array[i]; } }
    public int length { get { return array.Length; } }
}

[System.Serializable]
public class MfccData
{
    public string name;
    public List<MfccCalibrationData> mfccCalibrationDataList = new List<MfccCalibrationData>();
    public NativeArray<float> mfccNativeArray;

    public MfccData(string name)
    {
        this.name = name;
    }

    ~MfccData()
    {
        Deallocate();
    }

    public void Allocate()
    {
        if (IsAllocated()) return;

        mfccNativeArray = new NativeArray<float>(12, Allocator.Persistent);
    }

    public void Deallocate()
    {
        if (!IsAllocated()) return;

        mfccNativeArray.Dispose();
    }

    bool IsAllocated()
    {
        return mfccNativeArray.IsCreated;
    }

    public void AddCalibrationData(float[] mfcc)
    {
        if (mfcc.Length != 12)
        {
            Debug.LogError("The length of MFCC array should be 12.");
            return;
        }
        mfccCalibrationDataList.Add(new MfccCalibrationData() { array = mfcc });
    }

    public void RemoveOldCalibrationData(int dataCount)
    {
        while (mfccCalibrationDataList.Count > dataCount) mfccCalibrationDataList.RemoveAt(0);
    }

    public void UpdateNativeArray()
    {
        if (mfccCalibrationDataList.Count == 0) return;

        for (int i = 0; i < 12; ++i)
        {
            mfccNativeArray[i] = 0f;
            foreach (var mfcc in mfccCalibrationDataList)
            {
                mfccNativeArray[i] += mfcc[i];
            }
            mfccNativeArray[i] /= mfccCalibrationDataList.Count;
        }
    }

    public float GetAverage(int i)
    {
        return mfccNativeArray[i];
    }
}

[CreateAssetMenu(menuName = Common.assetName + "/Profile")]
public class Profile : ScriptableObject
{
    [HideInInspector] public string jsonPath = "";

    [Tooltip("The number of MFCC data to calculate the average MFCC values")]
    public int mfccDataCount = 16;
    [Tooltip("The number of Mel Filter Bank channels")]
    public int melFilterBankChannels = 80;
    [Tooltip("Target sampling rate to apply downsampling")]
    public int targetSampleRate = 16000;
    [Tooltip("Number of audio samples after downsampling is applied")]
    public int sampleCount = 1024;

    public List<MfccData> mfccs = new List<MfccData>();

    void OnEnable()
    {
        foreach (var data in mfccs)
        {
            data.Allocate();
            data.RemoveOldCalibrationData(mfccDataCount);
            data.UpdateNativeArray();
        }
    }

    void OnDisable()
    {
        foreach (var data in mfccs)
        {
            data.Deallocate();
        }
    }

    public string GetPhoneme(int index)
    {
        if (index < 0 || index >= mfccs.Count) return "";
        
        return mfccs[index].name;
    }

    public void AddMfcc(string name)
    {
        var data = new MfccData(name);
        data.Allocate();
        for (int i = 0; i < mfccDataCount; ++i)
        {
            data.AddCalibrationData(new float[12]);
        }
        mfccs.Add(data);
    }

    public void RemoveMfcc(int index)
    {
        if (index < 0 || index >= mfccs.Count) return;
        var data = mfccs[index];
        data.Deallocate();
        mfccs.RemoveAt(index);
    }

    public void UpdateMfcc(int index, NativeArray<float> mfcc, bool calib)
    {
        if (index < 0 || index >= mfccs.Count) return;

        var array = new float[mfcc.Length];
        mfcc.CopyTo(array);

        var data = mfccs[index];
        data.AddCalibrationData(array);
        data.RemoveOldCalibrationData(mfccDataCount);

        if (calib) data.UpdateNativeArray();
    }

    public NativeArray<float> GetAverages(int index)
    {
        return mfccs[index].mfccNativeArray;
    }

    public bool Export(string path)
    {
        var json = JsonUtility.ToJson(this);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }

        return true;
    }

    public bool Import(string path)
    {
        string json = "";

        try
        {
            json = File.ReadAllText(path);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }

        JsonUtility.FromJsonOverwrite(json, this);
        OnEnable();

        return true;
    }

    public string[] GetPhonemeNames()
    {
        return mfccs.Select(x => x.name).Distinct().ToArray();
    }

    public void CalcMinMax(out float min, out float max)
    {
        max = float.MinValue;
        min = float.MaxValue;
        foreach (var data in mfccs)
        {
            for (int j = 0; j < data.mfccCalibrationDataList.Count; ++j)
            {
                var array = data.mfccCalibrationDataList[j].array;
                for (int i = 0; i < array.Length; ++i)
                {
                    var x = array[i];
                    max = Mathf.Max(max, x);
                    min = Mathf.Min(min, x);
                }
            }
        }
    }

    public static Profile Create()
    {
        return ScriptableObject.CreateInstance<Profile>();
    }
}

}
