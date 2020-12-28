using UnityEngine;

namespace uLipSync
{

[CreateAssetMenu(menuName = "uLipSync/Config")]
public class Config : ScriptableObject
{
    public int lpcOrder = 64;
    public int sampleCount = 1024;
    public bool useSecondDerivative = true;
    public bool checkThirdFormant = true;
    public float filterH = 0f;
}

}
