using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public struct LipSyncJob2 : IJob
{
    public struct Result
    {
        public Vowel vowel;
        public float volume;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int lpcOrder;
    [ReadOnly] public int sampleRate;
    [ReadOnly] public float volumeThresh;
    [ReadOnly] public int maxFreq;
    [ReadOnly] public WindowFunc windowFunc;
    public NativeArray<float> H;
    public NativeArray<float> a;
    public NativeArray<float> i;
    public NativeArray<float> u;
    public NativeArray<float> e;
    public NativeArray<float> o;
    public NativeArray<Result> result;

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(ref input);
        if (volume < volumeThresh)
        {
            var res1 = result[0];
            res1.volume = volume;
            result[0] = res1;
            return;
        }

        // copy input ring buffer to a temporary array
        var data = new NativeArray<float>(input.Length, Allocator.Temp);
        Algorithm.CopyRingBuffer(ref input, ref data, startIndex);

        // multiply window function
        Algorithm.ApplyWindow(ref data, windowFunc);

        // auto correlational function
        var r = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        for (int l = 0; l < lpcOrder + 1; ++l)
        {
            for (int n = 0; n < input.Length - l; ++n)
            {
                r[l] += data[n] * data[n + l];
            }
        }

        data.Dispose();

        // calculate LPC factors using Levinson-Durbin algorithm
        var a = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        var e = new NativeArray<float>(lpcOrder + 1, Allocator.Temp);
        a[0] = 1f;
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
            U[k + 1] = 0f;
            V[k + 1] = 1f;
            for (int i = 1; i < k + 1; ++i)
            {
                U[i] = V[k + 1 - i] = a[i];
            }

            for (int i = 0; i < k + 2; ++i)
            {
                a[i] = U[i] + lambda * V[i];
            }

            e[k + 1] = e[k] * (1f - lambda * lambda);

            U.Dispose();
            V.Dispose();
        }

        r.Dispose();

        // calculate frequency characteristics
        int Nf = (int)((float)H.Length * sampleRate / maxFreq);
        float numerator = math.sqrt(math.abs(e[e.Length - 1]));
        for (int n = 0; n < H.Length; ++n)
        {
            float nr = 0f, ni = 0f, dr = 0f, di = 0f;
            for (int i = 0; i < lpcOrder + 1; ++i)
            {
                float theta = -2f * math.PI * i * n / Nf;
                float re = math.cos(theta);
                float im = math.sin(theta);
                nr += e[lpcOrder - i] * re;
                ni += e[lpcOrder - i] * im;
                dr += a[lpcOrder - i] * re;
                di += a[lpcOrder - i] * im;
            }
            float denominator = math.sqrt(dr * dr + di * di);
            if (denominator > math.EPSILON)
            {
                H[n] = numerator / denominator;
            }
        }

        a.Dispose();
        e.Dispose();

        var res = new Result();
        float minError = float.MaxValue;
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var vowel = (Vowel)i;
            var error = GetError(vowel);
            if (error < minError)
            {
                res.vowel = vowel;
                minError = error;
            }
        }
        res.volume = volume;
        result[0] = res;
    }

    NativeArray<float> GetVowelArray(Vowel vowel)
    {
        switch (vowel)
        {
            case Vowel.A: return a;
            case Vowel.I: return i;
            case Vowel.U: return u;
            case Vowel.E: return e;
            case Vowel.O: return o;
            default: return a;
        }
    }

    float GetError(Vowel vowel)
    {
        var P = GetVowelArray(vowel);

        float sum = 0f;
        float maxH = Algorithm.GetMaxValue(ref H);
        float maxP = Algorithm.GetMaxValue(ref P);
        float min = math.log10(1e-2f);

        for (int i = 0; i < H.Length; ++i)
        {
            float h = H[i] / maxH;
            h = math.log10(10f * h);
            h = (h - min) / (1f - min);
            h = math.max(h, 0f);

            float p = P[i] / maxP;
            p = math.log10(10f * p);
            p = (p - min) / (1f - min);
            p = math.max(p, 0f);

            sum += math.pow(h - p, 2f);
        }

        return sum;
    }
}

}
