using UnityEngine;

namespace uLipSync
{

[CreateAssetMenu(menuName = Common.assetName + "/Config")]
public class Config : ScriptableObject
{
    [Min(16)] public int lpcOrder = 64;
    [Min(256)] public int sampleCount = 1024;
    public bool checkSecondDerivative = true;
    public bool checkThirdFormant = true;
    public float filterH = 0f;
}

}
