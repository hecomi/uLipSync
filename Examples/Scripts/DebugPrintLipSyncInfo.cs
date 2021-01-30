using UnityEngine;
using uLipSync;

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    public float threshVolume = 1e-5f;
    public bool outputLog = true;

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        if (info.volume > threshVolume && outputLog) 
        {
            Debug.LogFormat($"VOWEL: {info.vowel}, VOL: {info.volume}, DIST: {info.distance} ");
        }
    }
}
