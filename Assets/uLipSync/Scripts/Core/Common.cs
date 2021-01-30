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
}

public struct LipSyncInfo
{
    public Vowel vowel;
    public float volume;
    public float rawVolume;
    public float distance;
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
