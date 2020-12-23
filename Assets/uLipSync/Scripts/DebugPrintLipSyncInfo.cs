using UnityEngine;

namespace uLipSync.Samples
{

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    public float volThresh  = 0.0001f;

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        if (info.volume > 0.0001f) 
        {
            Debug.LogFormat("VOWEL: {0}, VOL: {1}, FORMANT: {2}, {3}",
                info.vowel, info.volume, info.formant.f1, info.formant.f2);
        }
    }
}

}
