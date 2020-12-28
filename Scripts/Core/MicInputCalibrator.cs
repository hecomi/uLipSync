using UnityEngine;

namespace uLipSync
{

[ExecuteInEditMode]
public class MicInputCalibrator : uLipSyncMicInput
{
    new void OnEnable()
    {
        isAutoStart = false;
        base.OnEnable();
    }
}

}
