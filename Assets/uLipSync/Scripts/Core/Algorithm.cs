using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public static class Algorithm
{
    [BurstCompile]
    public static float GetMaxValue(NativeArray<float> array)
    {
        float max = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            max = math.max(max, math.abs(array[i]));
        }
        return max;
    }

    [BurstCompile]
    public static float GetMinValue(NativeArray<float> array)
    {
        float min = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            min = math.min(min, math.abs(array[i]));
        }
        return min;
    }

    [BurstCompile]
    public static float GetRMSVolume(NativeArray<float> array)
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
    public static void CopyRingBuffer(NativeArray<float> src, NativeArray<float> dst, int startSrcIndex)
    {
        int sn = src.Length;
        int dn = dst.Length;
        for (int i = 0; i < dn; ++i)
        {
            dst[i] = src[(startSrcIndex + i) % sn];
        }
    }

    [BurstCompile]
    public static void Normalize(NativeArray<float> array)
    {
        float max = GetMaxValue(array);
        if (max < math.EPSILON) return;
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] /= max;
        }
    }

    [BurstCompile]
    public static void HammingWindow(NativeArray<float> array)
    {
        int N = array.Length;

        for (int i = 0; i < N; ++i)
        {
            float x = (float)i / (N - 1);
            array[i] *= 0.54f - 0.46f * math.cos(2f * math.PI * x);
        }
    }

    [BurstCompile]
    public static void Fft(NativeArray<float> data, NativeArray<float> spectrum)
    {
        int N = data.Length;

        var spectrumComplex = new NativeArray<float2>(N, Allocator.Temp);
        for (int i = 0; i < N; ++i)
        {
            spectrumComplex[i] = new float2(data[i], 0f);
        }
        Fft(spectrumComplex, N);

        for (int i = 0; i < N; ++i)
        {
            spectrum[i] = math.length(spectrumComplex[i]);
        }

        data.Dispose();
        spectrumComplex.Dispose();
    }

    [BurstCompile]
    static void Fft(NativeArray<float2> spectrum, int N)
    {
        if (N < 2) return;

        var even = new NativeArray<float2>(N / 2, Allocator.Temp);
        var odd = new NativeArray<float2>(N / 2, Allocator.Temp);

        for (int i = 0; i < N / 2; ++i)
        {
            even[i] = spectrum[i * 2];
            odd[i] = spectrum[i * 2 + 1];
        }

        Fft(even, N / 2);
        Fft(odd, N / 2);

        for (int i = 0; i < N / 2; ++i)
        {
            var e = even[i];
            var o = odd[i];
            float theta = -2f * math.PI * i / N;
            var c = new float2(math.cos(theta), math.sin(theta));
            c = new float2(c.x * o.x - c.y * o.y, c.x * o.y + c.y * o.x);
            spectrum[i] = e + c;
            spectrum[N / 2 + i] = e - c;
        }

        even.Dispose();
        odd.Dispose();
    }

    [BurstCompile]
    public static void MelFilterBankLog10(
        NativeArray<float> melSpectrum, 
        NativeArray<float> spectrum, 
        float sampleRate,
        int melDiv)
    {
        float fMax = sampleRate / 2;
        float melMax = ToMel(fMax);
        int nMax = spectrum.Length / 2;
        float df = fMax / nMax;
        float dMel = melMax / (melDiv + 1);

        for (int n = 0; n < melDiv; ++n)
        {
            float melBegin = dMel * n;
            float melCenter = dMel * (n + 1);
            float melEnd = dMel * (n + 2);

            float fBegin = ToHz(melBegin);
            float fCenter = ToHz(melCenter);
            float fEnd = ToHz(melEnd);

            int iBegin = (int)math.round(fBegin / df);
            int iCenter = (int)math.round(fCenter / df);
            int iEnd = (int)math.round(fEnd / df);

            float sum = 0f;
            for (int i = iBegin + 1; i < iEnd; ++i)
            {
                float a = (i < iCenter) ? ((float)i / iCenter) : ((float)(i - iCenter) / iCenter);
                sum += a * spectrum[i];
            }
            melSpectrum[n] = math.log10(sum);
        }
    }

    [BurstCompile]
    public static float ToMel(float hz)
    {
        return 1127.010480f * math.log(hz / 700f + 1f);
    }

    [BurstCompile]
    public static float ToHz(float mel)
    {
        return 700f * (math.exp(mel / 1127.010480f) - 1f);
    }

    [BurstCompile]
    public static void DCT(
        NativeArray<float> cepstrum,
        NativeArray<float> spectrum)
    {
        int N = cepstrum.Length;
        float a = math.PI / N;
        for (int i = 0; i < N; ++i)
        {
            float sum = 0f;
            for (int j = 0; j < N; ++j)
            {
                float ang = (j + 0.5f) * i * a;
                sum += spectrum[j] * math.cos(ang);
            }
            cepstrum[i] = sum;
        }
    }

    /*
    [BurstCompile]
    public static float Cov(List<NativeArray<float>> data)
    {
    // 分散共分散行列を求める
    }
    */
}

}
