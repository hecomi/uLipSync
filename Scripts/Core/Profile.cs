using UnityEngine;

namespace uLipSync
{

[CreateAssetMenu(menuName = Common.assetName + "/Profile")]
public class Profile : ScriptableObject
{
    public FormantPair formantA = Common.averageFormantMan[Vowel.A];
    public FormantPair formantI = Common.averageFormantMan[Vowel.I];
    public FormantPair formantU = Common.averageFormantMan[Vowel.U];
    public FormantPair formantE = Common.averageFormantMan[Vowel.E];
    public FormantPair formantO = Common.averageFormantMan[Vowel.O];
    public float maxError = 500f; // Hz
    public float minLog10H = -1f; // Hz

#if UNITY_EDITOR
    public static bool foldOutFormant = true;
    public static bool foldOutTips = true;
    public static bool foldOutVisualizer = true;
    public static bool foldOutSettings = true;
#endif
}

}
