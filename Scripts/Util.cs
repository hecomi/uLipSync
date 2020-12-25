using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public static class Util
{
    public static Vowel GetVowel(FormantPair formant, Config config)
    {
        float diffA = FormantPair.Dist(formant, config.formantA);
        float diffI = FormantPair.Dist(formant, config.formantI);
        float diffU = FormantPair.Dist(formant, config.formantU);
        float diffE = FormantPair.Dist(formant, config.formantE);
        float diffO = FormantPair.Dist(formant, config.formantO);
        float minDiff = math.min(diffA, math.min(diffI, math.min(diffU, math.min(diffE, diffO))));
        if (minDiff > config.maxError) return Vowel.None;

        if      (diffA == minDiff) { return Vowel.A; }
        else if (diffI == minDiff) { return Vowel.I; }
        else if (diffU == minDiff) { return Vowel.U; }
        else if (diffE == minDiff) { return Vowel.E; }
        else if (diffO == minDiff) { return Vowel.O; }

        return Vowel.None;
    }
}

}
