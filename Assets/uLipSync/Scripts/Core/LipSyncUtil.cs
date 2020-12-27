using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

public struct VowelInfo
{
    public Vowel vowel;
    public float diff;
    public FormantPair formant;
}

[BurstCompile]
public static class LipSyncUtil
{
    public static VowelInfo GetVowel(FormantPair formant, Profile profile)
    {
        var info = new VowelInfo();
        info.vowel = Vowel.None;
        info.formant = formant;

        float diffA = FormantPair.Dist(formant, profile.formantA);
        float diffI = FormantPair.Dist(formant, profile.formantI);
        float diffU = FormantPair.Dist(formant, profile.formantU);
        float diffE = FormantPair.Dist(formant, profile.formantE);
        float diffO = FormantPair.Dist(formant, profile.formantO);

        float minDiff = math.min(diffA, math.min(diffI, math.min(diffU, math.min(diffE, diffO))));
        info.diff = minDiff;

        if (minDiff < profile.maxError)
        {
            if      (diffA == minDiff) { info.vowel = Vowel.A; }
            else if (diffI == minDiff) { info.vowel = Vowel.I; }
            else if (diffU == minDiff) { info.vowel = Vowel.U; }
            else if (diffE == minDiff) { info.vowel = Vowel.E; }
            else if (diffO == minDiff) { info.vowel = Vowel.O; }
        }

        return info;
    }

    public static VowelInfo GetVowel(float f1, float f2, float f3, Profile config)
    {
        var result12 = GetVowel(new FormantPair(f1, f2), config);
        var result23 = GetVowel(new FormantPair(f2, f3), config);
        var result13 = GetVowel(new FormantPair(f1, f3), config);
        var minDiff = math.min(math.min(result12.diff, result23.diff), result13.diff);
        return 
            math.abs(result12.diff - minDiff) < 1f ? result12 :
            math.abs(result23.diff - minDiff) < 1f ? result23 :
            result13;
    }
}

}
