using UnityEngine.Events;
using System.Collections.Generic;

namespace uLipSync
{

public static class Common
{
    public const string AssetName = "uLipSync";
    public const float DefaultMinVolume = -2.5f;
    public const float DefaultMaxVolume = -1.5f;
    public const float MfccMinValue = -50f;
    public const float MfccMaxValue = 30f;
}

public struct LipSyncInfo
{
    public string phoneme;
    public float volume;
    public float rawVolume;
    public Dictionary<string, float> phonemeRatios;
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

[System.Serializable]
public class AudioFilterReadEvent : UnityEvent<float[], int> 
{
}

public enum UpdateMethod
{
    LateUpdate,
    Update,
    FixedUpdate,
    LipSyncUpdateEvent,
    External,
}

public enum CompareMethod
{
    L1Norm,
    L2Norm,
    CosineSimilarity,
}

}
