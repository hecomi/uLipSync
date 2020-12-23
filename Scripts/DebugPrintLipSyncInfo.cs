using UnityEngine;

namespace uLipSync.Samples
{

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    [Range(-160f, 20f), Tooltip("dB")]
    public float threshVolume = 0f;

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
