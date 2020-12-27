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
            Debug.LogFormat("MAIN VOWEL: {0}, [ A:{1} I:{2}, U:{3} E:{4} O:{5} N:{6} ], VOL: {7}, FORMANT: {8}, {9}",
                info.mainVowel, 
                info.volume, 
                info.vowels[Vowel.A],
                info.vowels[Vowel.I],
                info.vowels[Vowel.U],
                info.vowels[Vowel.E],
                info.vowels[Vowel.O],
                info.vowels[Vowel.None],
                info.formant.f1, 
                info.formant.f2);
        }
    }
}

}
