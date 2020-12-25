using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

public struct CalcFormantsResult
{
    public FormantPair formant;
    public float volume;
}

[BurstCompile]
public struct CalcFormantsJob : IJob
{
    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int lpcOrder;
    [ReadOnly] public float deltaFreq;
    [ReadOnly] public float volumeThresh;
    public NativeArray<float> H;
    [WriteOnly] public NativeArray<CalcFormantsResult> result;

    public void Execute()
    {
        int N = input.Length;

        // skip if volume is smaller than threshold
        float volume = Algorithm.GetRMSVolume(ref input);
        if (volume < volumeThresh) return;

        // copy input ring buffer to a temporary array
        var data = new NativeArray<float>(N, Allocator.Temp);
        Algorithm.CopyRingBuffer(ref input, ref data, startIndex);

        // multiply hamming window function
        for (int i = 1; i < N - 1; ++i)
        {
            data[i] *= 0.54f - 0.46f * math.cos(2f * math.PI * i / (N - 1));
        }

        // auto correlational function
        var r = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        for (int l = 0; l < lpcOrder + 1; ++l)
        {
            r[l] = 0f;
            for (int n = 0; n < N - l; ++n)
            {
                r[l] += data[n] * data[n + l];
            }
        }

        // calculate LPC factors using Levinson-Durbin algorithm
        var a = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        var e = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        for (int i = 0; i < lpcOrder + 1; ++i)
        {
            a[i] = e[i] = 0f;
        }
        a[0] = e[0] = 1f;
        a[1] = -r[1] / r[0];
        e[1] = r[0] + r[1] * a[1];
        for (int k = 1; k < lpcOrder; ++k)
        {
            float lambda = 0f;
            for (int j = 0; j < k + 1; ++j)
            {
                lambda -= a[j] * r[k + 1 - j];
            }
            lambda /= e[k];

            var U = new NativeArray<float>(k + 2, Allocator.Temp);
            var V = new NativeArray<float>(k + 2, Allocator.Temp);

            U[0] = 1f;
            V[0] = 0f;
            for (int i = 1; i < k + 1; ++i)
            {
                U[i] = a[i];
                V[k + 1 - i] = a[i];
            }
            U[k + 1] = 0f;
            V[k + 1] = 1f;

            for (int i = 0; i < k + 2; ++i)
            {
                a[i] = U[i] + lambda * V[i];
            }

            e[k + 1] = e[k] * (1f - lambda * lambda);

            U.Dispose();
            V.Dispose();
        }

        // calculate frequency characteristics
        var Htmp = new NativeArray<float>(N, Allocator.Temp);
        for (int n = 0; n < N; ++n)
        {
            float nr = 0f, ni = 0f, dr = 0f, di = 0f;
            for (int i = 0; i < lpcOrder + 1; ++i)
            {
                float re = math.cos(-2f * math.PI * n * i / N);
                float im = math.sin(-2f * math.PI * n * i / N);
                nr += e[lpcOrder - i] * re;
                ni += e[lpcOrder - i] * im;
                dr += a[lpcOrder - i] * re;
                di += a[lpcOrder - i] * im;
            }
            float numerator = math.sqrt(math.pow(nr, 2f) + math.pow(ni, 2f));
            float denominator = math.sqrt(math.pow(dr, 2f) + math.pow(di, 2f));
            if (denominator > math.EPSILON)
            {
                Htmp[n] = numerator / denominator;
            }
        }

        Algorithm.Normalize(ref Htmp);
        for (int i = 0; i < N; ++i)
        {
            H[i] += (Htmp[i] - H[i]) * 0.35f;
        }

        data.Dispose();
        r.Dispose();
        a.Dispose();
        e.Dispose();
        Htmp.Dispose();

        // get first and second formants
        var formant = new FormantPair();
        bool foundFirst = false;
        for (int i = 1; i < N - 1; ++i)
        {
            if (H[i] > H[i - 1] && H[i] > H[i + 1])
            {
                if (!foundFirst)
                {
                    formant.f1 = i * deltaFreq;
                    foundFirst = true;
                }
                else
                {
                    formant.f2 = i * deltaFreq;
                    break;
                }
            }
        }

        result[0] = new CalcFormantsResult()
        {
            volume = volume,
            formant = formant,
        };
    }
}

}
