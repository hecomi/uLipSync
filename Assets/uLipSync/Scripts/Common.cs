using UnityEngine;
using UnityEngine.Events;

namespace uLipSync
{

public enum Vowel
{
    None,
    A,
    I,
    U,
    E,
    O,
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

public struct LipSyncInfo
{
    public float volume;
    public Vowel vowel;
    public FormantPair formant;
}

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
