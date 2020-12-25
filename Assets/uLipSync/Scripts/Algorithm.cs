using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public static class Algorithm
{
    [BurstCompile]
    public static float GetMaxValue(ref NativeArray<float> array)
    {
        float max = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            max = math.max(max, math.abs(array[i]));
        }
        return max;
    }

    [BurstCompile]
    public static float GetVolume(float maxAmp)
    {
        float refAmp = 0.1f;
        return math.max(20f * math.log10(maxAmp / refAmp), -160f);
    }

    [BurstCompile]
    public static void Normalize(ref NativeArray<float> array)
    {
        float max = GetMaxValue(ref array);
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] /= max;
        }
    }
}

}
