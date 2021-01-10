using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace uLipSync
{

public enum Vowel
{
    A,
    I,
    U,
    E,
    O,
    None,
}

public enum WindowFunc
{
    None,
    Hann,
    BlackmanHarris,
    Gaussian4_5,
}

[System.Serializable]
public struct FormantPair
{
    public float f1;
    public float f2;

    public FormantPair(float f1, float f2)
    {
        this.f1 = f1;
        this.f2 = f2;
    }

    public static float Dist(FormantPair a, FormantPair b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.f1 - b.f1, 2f) + Mathf.Pow(a.f2 - b.f2, 2f));
    }
}

public class LipSyncInfo
{
    public float volume = 0f;
    public Vowel mainVowel = Vowel.None;
    public Dictionary<Vowel, float> vowels = new Dictionary<Vowel, float>()
    {
        { Vowel.A, 0f },
        { Vowel.I, 0f },
        { Vowel.U, 0f },
        { Vowel.E, 0f },
        { Vowel.O, 0f },
        { Vowel.None, 1f },
    };
    public FormantPair formant = new FormantPair();
}

public static class Common
{
    public const string assetName = "uLipSync";

    public const string defaultProfileMan = assetName + "-Profile-Man";
    public const string defaultProfileWoman = assetName + "-Profile-Woman";
    public const string defaultConfig = assetName + "-Config-Default";
    public const string calibrationConfig = assetName + "-Config-Calibration";

    // Ref: http://bousure639.gjgd.net/Entry/164/
    public static Dictionary<Vowel, FormantPair> averageFormantMan = new Dictionary<Vowel, FormantPair>()
    {
        { Vowel.A, new FormantPair(775, 1163) },
        { Vowel.I, new FormantPair(263, 2263) },
        { Vowel.U, new FormantPair(363, 1300) },
        { Vowel.E, new FormantPair(475, 1738) },
        { Vowel.O, new FormantPair(550, 838) },
    };
    public static Dictionary<Vowel, FormantPair> averageFormantWoman = new Dictionary<Vowel, FormantPair>()
    {
        { Vowel.A, new FormantPair(888, 1363) },
        { Vowel.I, new FormantPair(325, 2725) },
        { Vowel.U, new FormantPair(375, 1675) },
        { Vowel.E, new FormantPair(483, 2317) },
        { Vowel.O, new FormantPair(483, 925) },
    };
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
