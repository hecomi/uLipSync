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
    public static VowelInfo GetVowel(FormantPair formant, Config config)
    {
        var info = new VowelInfo();
        info.vowel = Vowel.None;
        info.formant = formant;

        float diffA = FormantPair.Dist(formant, config.formantA);
        float diffI = FormantPair.Dist(formant, config.formantI);
        float diffU = FormantPair.Dist(formant, config.formantU);
        float diffE = FormantPair.Dist(formant, config.formantE);
        float diffO = FormantPair.Dist(formant, config.formantO);

        float minDiff = math.min(diffA, math.min(diffI, math.min(diffU, math.min(diffE, diffO))));
        if (minDiff < config.maxError)
        {
            if      (diffA == minDiff) { info.vowel = Vowel.A; }
            else if (diffI == minDiff) { info.vowel = Vowel.I; }
            else if (diffU == minDiff) { info.vowel = Vowel.U; }
            else if (diffE == minDiff) { info.vowel = Vowel.E; }
            else if (diffO == minDiff) { info.vowel = Vowel.O; }
        }

        return info;
    }

    public static VowelInfo GetVowel(float f1, float f2, float f3, Config config)
    {
        var result12 = GetVowel(new FormantPair(f1, f2), config);
        var result23 = GetVowel(new FormantPair(f2, f3), config);
        var result13 = GetVowel(new FormantPair(f1, f3), config);
        var minDiff = math.min(math.min(result12.diff, result23.diff), result13.diff);
        return 
            (result12.diff == minDiff) ? result12 :
            (result23.diff == minDiff) ? result23 :
            result13;
    }
}

}
