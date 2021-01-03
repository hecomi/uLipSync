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
        if (max < math.EPSILON) return;
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] /= max;
        }
    }

    [BurstCompile]
    public static void ApplyWindow(ref NativeArray<float> array, WindowFunc windowFunc)
    {
        int N = array.Length;

        switch (windowFunc)
        {
            case WindowFunc.Hann: 
            {
                for (int i = 0; i < N; ++i)
                {
                    float x = (float)i / (N - 1);
                    array[i] *= 0.5f - 0.5f * math.cos(2f * math.PI * x);
                }
                break;
            }
            case WindowFunc.BlackmanHarris: 
            {
                for (int i = 0; i < N; ++i)
                {
                    float x = (float)i / (N - 1);
                    array[i] *= 
                        0.35875f 
                        - 0.48829f * math.cos(2f * math.PI * x)
                        + 0.14128f * math.cos(4f * math.PI * x)
                        - 0.01168f * math.cos(6f * math.PI * x);
                }
                break;
            }
            case WindowFunc.Gaussian4_5: 
            {
                for (int i = 0; i < N; ++i)
                {
                    float x = (float)i / (N - 1);
                    array[i] *= math.exp(-math.pow(x / 4.5f, 2f));
                }
                break;
            }
        }
    }
}

}
