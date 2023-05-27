using UnityEngine;
using uLipSync;

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        if (!isActiveAndEnabled) return;

        if (info.volume < Mathf.Epsilon) return;

        Debug.LogFormat($"PHENOME: {info.phoneme}, VOL: {info.volume} ");
    }
}
