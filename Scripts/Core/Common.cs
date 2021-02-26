using UnityEngine.Events;

namespace uLipSync
{

public static class Common
{
    public const string assetName = "uLipSync";
    public const string defaultProfileMan = assetName + "-Profile-Man";
    public const string defaultProfileWoman = assetName + "-Profile-Woman";
}

public struct LipSyncInfo
{
    public int index;
    public string phoneme;
    public float volume;
    public float rawVolume;
    public float distance;
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
