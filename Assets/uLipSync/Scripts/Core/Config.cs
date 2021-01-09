using UnityEngine;

namespace uLipSync
{

[CreateAssetMenu(menuName = Common.assetName + "/Config")]
public class Config : ScriptableObject
{
    [Min(256)] public int sampleCount = 1024;
    [Min(16)] public int lpcOrder = 64;
    [Min(32)] public int frequencyResolution = 128;
    [Min(3000)] public int maxFrequency = 4000;
    public WindowFunc windowFunc = WindowFunc.Hann;
    public bool checkSecondDerivative = false;
    public bool checkThirdFormant = true;
    public float filterH = 0f;
}

}
