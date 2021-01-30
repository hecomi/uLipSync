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

        if (Input.GetKey(KeyCode.A)) lipSync.RequestCalibration(Vowel.A);
        if (Input.GetKey(KeyCode.I)) lipSync.RequestCalibration(Vowel.I);
        if (Input.GetKey(KeyCode.U)) lipSync.RequestCalibration(Vowel.U);
        if (Input.GetKey(KeyCode.E)) lipSync.RequestCalibration(Vowel.E);
        if (Input.GetKey(KeyCode.O)) lipSync.RequestCalibration(Vowel.O);
    }
}

}
