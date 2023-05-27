using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(uLipSync))]
public class CalibrationByKeyboardInput : MonoBehaviour
{
    uLipSync _lipSync;

    void Start()
    {
        _lipSync = GetComponent<uLipSync>();
    }

    void Update()
    {
        if (!_lipSync) return;

        for (int i = 0; i < _lipSync.profile.mfccs.Count; ++i)
        {
            var key = (KeyCode)((int)(KeyCode.Alpha1) + i);
            if (Input.GetKey(key)) _lipSync.RequestCalibration(i);
        }
    }
}

}
