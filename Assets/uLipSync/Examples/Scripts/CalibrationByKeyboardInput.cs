using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(uLipSync))]
public class CalibrationByKeyboardInput : MonoBehaviour
{
    uLipSync lipSync;

    void Start()
    {
        lipSync = GetComponent<uLipSync>();
    }

    void Update()
    {
        if (!lipSync) return;

        for (int i = 0; i < lipSync.profile.mfccs.Count; ++i)
        {
            var key = (KeyCode)((int)(KeyCode.Alpha1) + i);
            if (Input.GetKey(key)) lipSync.RequestCalibration(i);
        }
    }
}

}
