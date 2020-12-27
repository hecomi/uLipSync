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

[System.Serializable]
public class LipSyncUpdateEvent : UnityEvent<LipSyncInfo> 
{
}

}
