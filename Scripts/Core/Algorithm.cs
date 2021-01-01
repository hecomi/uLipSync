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
    public static float GetMinValue(ref NativeArray<float> array)
    {
        float min = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            min = math.min(min, math.abs(array[i]));
        }
        return min;
    }

    [BurstCompile]
    public static float GetRMSVolume(ref NativeArray<float> array)
    {
        float average = 0f;
        int n = array.Length;
        for (int i = 0; i < n; ++i)
        {
            average += array[i] * array[i];
        }
        return math.sqrt(average / n);
    }

    [BurstCompile]
    public static void CopyRingBuffer(ref NativeArray<float> src, ref NativeArray<float> dst, int startSrcIndex)
    {
        int sn = src.Length;
        int dn = dst.Length;
        for (int i = 0; i < dn; ++i)
        {
            int index = (startSrcIndex + i) % sn;
            dst[i] = src[index];
        }
    }

    [BurstCompile]
    public static void Normalize(ref NativeArray<float> array)
    {
        float max = GetMaxValue(ref array);
        if (math.abs(max) < math.EPSILON) return;
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] /= max;
        }
    }
}

}
