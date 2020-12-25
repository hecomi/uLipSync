using UnityEngine;

namespace uLipSync.Samples
{

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    [Range(0f, 1f), Tooltip("RMS Volume")]
    public float threshVolume = 0.01f;

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        if (info.volume > threshVolume) 
        {
            Debug.LogFormat("VOWEL: {0}, VOL: {1}, FORMANT: {2}, {3}",
                info.vowel, info.volume, info.formant.f1, info.formant.f2);
        }
    }
}

}
