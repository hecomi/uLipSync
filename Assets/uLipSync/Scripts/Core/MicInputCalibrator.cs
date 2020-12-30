using UnityEngine;

namespace uLipSync
{

[ExecuteInEditMode]
public class MicInputCalibrator : uLipSyncMicrophone
{
    new void OnEnable()
    {
        isAutoStart = false;
        base.OnEnable();
    }
}

}
