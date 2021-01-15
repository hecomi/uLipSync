using UnityEngine;
using UnityEngine.Events;

namespace uLipSync
{

public enum Vowel
{
    A = 0,
    I,
    U,
    E,
    O,
    None,
}

public static class Common
{
    public const string assetName = "uLipSync";
    public const int sampleCount = 1024;
}

public struct LipSyncInfo
{
    public string phenome;
    public float volume;
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
