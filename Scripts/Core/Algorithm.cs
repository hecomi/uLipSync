using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public static class Algorithm
{
    [BurstCompile]
    public static float GetMaxValue(in NativeArray<float> array)
    {
        float max = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            max = math.max(max, math.abs(array[i]));
        }
        return max;
    }

    [BurstCompile]
    public static float GetMinValue(in NativeArray<float> array)
    {
        float min = 0f;
        for (int i = 0; i < array.Length; ++i)
        {
            min = math.min(min, math.abs(array[i]));
        }
        return min;
    }

    [BurstCompile]
    public static float GetRMSVolume(in NativeArray<float> array)
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
    public static void CopyRingBuffer(in NativeArray<float> input, out NativeArray<float> output, int startSrcIndex)
    {
        output = new NativeArray<float>(input.Length, Allocator.Temp);
        for (int i = 0; i < input.Length; ++i)
        {
            output[i] = input[(startSrcIndex + i) % input.Length];
        }
    }

    [BurstCompile]
    public static void Normalize(ref NativeArray<float> array)
    {
        float max = GetMaxValue(array);
        if (max < math.EPSILON) return;
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] /= max;
        }
    }

    [BurstCompile]
    public static void LowPassFilter(ref NativeArray<float> data, float sampleRate, float cutoff, float range)
    {
        cutoff /= sampleRate;
        range /= sampleRate;

        int n = (int)math.round(3.1f / range);
        if ((n + 1) % 2 == 0) n += 1;

        var b = new NativeArray<float>(n, Allocator.Temp);
        for (int i = 0; i < n; ++i)
        {
            float x = i - (n - 1) / 2f;
            float ang = 2f * math.PI * cutoff * x;
            b[i] = 2f * cutoff * math.sin(ang) / ang;
        }

        var tmp = new NativeArray<float>(data, Allocator.Temp);
        for (int i = 0; i < data.Length; ++i)
        {
            for (int j = 0; j < b.Length; ++j)
            {
                if (i - j >= 0)
                {
                    data[i] += b[j] * tmp[i - j];
                }
            }
        }
        tmp.Dispose();
    }

    [BurstCompile]
    public static void DownSample(in NativeArray<float> input, out NativeArray<float> output, int sampleRate, int targetSampleRate)
    {
        if (sampleRate <= targetSampleRate)
        {
            output = new NativeArray<float>(input, Allocator.Temp);
        }
        else if (sampleRate % targetSampleRate == 0)
        {
            int skip = sampleRate / targetSampleRate;
            output = new NativeArray<float>(input.Length / skip, Allocator.Temp);
            for (int i = 0; i < output.Length; ++i)
            {
                output[i] = input[i * skip];
            }
        }
        else
        {
            float df = (float)sampleRate / targetSampleRate;
            int n = (int)math.round(input.Length / df);
            output = new NativeArray<float>(n, Allocator.Temp);
            for (int j = 0; j < output.Length; ++j)
            {
                float fIndex = df * j;
                int i0 = (int)math.floor(fIndex);
                int i1 = math.min(i0, input.Length - 1);
                float t = fIndex - i0;
                float x0 = input[i0];
                float x1 = input[i1];
                output[j] = math.lerp(x0, x1, t);
            }
        }
    }

    [BurstCompile]
    public static void PreEmphasis(ref NativeArray<float> data, float p)
    {
        var tmp = new NativeArray<float>(data, Allocator.Temp);
        for (int i = 1; i < data.Length; ++i)
        {
            data[i] = tmp[i] - p * tmp[i - 1];
        }
        tmp.Dispose();
    }

    [BurstCompile]
    public static void HammingWindow(ref NativeArray<float> array)
    {
        int N = array.Length;

        for (int i = 0; i < N; ++i)
        {
            float x = (float)i / (N - 1);
            array[i] *= 0.54f - 0.46f * math.cos(2f * math.PI * x);
        }
    }

    [BurstCompile]
    public static void FFT(in NativeArray<float> data, out NativeArray<float> spectrum)
    {
        int N = data.Length;
        spectrum = new NativeArray<float>(N, Allocator.Temp);

        var spectrumRe = new NativeArray<float>(N, Allocator.Temp);
        var spectrumIm = new NativeArray<float>(N, Allocator.Temp);
        for (int i = 0; i < N; ++i)
        {
            spectrumRe[i] = data[i];
        }
        FFT(ref spectrumRe, ref spectrumIm, N);

        for (int i = 0; i < N; ++i)
        {
            float re = spectrumRe[i];
            float im = spectrumIm[i];
            spectrum[i] = math.length(new float2(re, im));
        }

        data.Dispose();
        spectrumRe.Dispose();
        spectrumIm.Dispose();
    }

    [BurstCompile]
    static void FFT(ref NativeArray<float> spectrumRe, ref NativeArray<float> spectrumIm, int N)
    {
        if (N < 2) return;

        var evenRe = new NativeArray<float>(N / 2, Allocator.Temp);
        var evenIm = new NativeArray<float>(N / 2, Allocator.Temp);
        var oddRe = new NativeArray<float>(N / 2, Allocator.Temp);
        var oddIm = new NativeArray<float>(N / 2, Allocator.Temp);

        for (int i = 0; i < N / 2; ++i)
        {
            evenRe[i] = spectrumRe[i * 2];
            evenIm[i] = spectrumIm[i * 2];
            oddRe[i] = spectrumRe[i * 2 + 1];
            oddIm[i] = spectrumIm[i * 2 + 1];
        }

        FFT(ref evenRe, ref evenIm, N / 2);
        FFT(ref oddRe, ref oddIm, N / 2);

        for (int i = 0; i < N / 2; ++i)
        {
            float er = evenRe[i];
            float ei = evenIm[i];
            float or = oddRe[i];
            float oi = oddIm[i];
            float theta = -2f * math.PI * i / N;
            var c = new float2(math.cos(theta), math.sin(theta));
            c = new float2(c.x * or - c.y * oi, c.x * oi + c.y * or);
            spectrumRe[i] = er + c.x;
            spectrumIm[i] = ei + c.y;
            spectrumRe[N / 2 + i] = er - c.x;
            spectrumIm[N / 2 + i] = ei - c.y;
        }

        evenRe.Dispose();
        evenIm.Dispose();
        oddRe.Dispose();
        oddIm.Dispose();
    }

    [BurstCompile]
    public static void MelFilterBank(
        in NativeArray<float> spectrum, 
        out NativeArray<float> melSpectrum,
        float sampleRate,
        int melDiv)
    {
        melSpectrum = new NativeArray<float>(melDiv, Allocator.Temp);

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
            melSpectrum[n] = sum;
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
        in NativeArray<float> spectrum,
        out NativeArray<float> cepstrum)
    {
        int N = spectrum.Length;
        cepstrum = new NativeArray<float>(N, Allocator.Temp);
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
}

}
