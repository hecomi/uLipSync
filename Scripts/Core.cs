using UnityEngine;

namespace uLipSync
{

public static class Core
{
    public static float GetVolume(float[] input)
    {
        float vol = 0f;
        for (int i = 0; i < input.Length; ++i)
        {
            vol += input[i] * input[i];
        }
        vol /= input.Length;
        return vol;
    }

    public static FormantPair GetFormants(float[] input, Config config, float deltaFreq)
    {
        var data = new float[input.Length];
        System.Array.Copy(input, 0, data, 0, input.Length);

        int N = data.Length;
        int order = config.lpcOrder;

        // multiply hamming window function
        for (int i = 1; i < N - 1; ++i)
        {
            data[i] *= 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (N - 1));
        }
        data[0] = data[N - 1] = 0f;

        // normalize
        float max = 0f, min = 0f;
        for (int i = 0; i < N; ++i)
        {
            if (data[i] > max) max = data[i];
            if (data[i] < min) min = data[i];
        }
        max = Mathf.Abs(max);
        min = Mathf.Abs(min);
        float factor = 1f;
        if (max > min) factor = 1f / max;
        if (max < min) factor = 1f / min;
        for (int i = 0; i < N; ++i)
        {
            data[i] *= factor;
        }

        // auto correlational function
        var r = new float[order + 1];
        for (int l = 0; l < order + 1; ++l)
        {
            r[l] = 0f;
            for (int n = 0; n < N - l; ++n)
            {
                r[l] += data[n] * data[n + l];
            }
        }

        // calculate LPC factors using Levinson-Durbin algorithm
        var a = new float[order + 1];
        var e = new float[order + 1];
        for (int i = 0; i < order + 1; ++i)
        {
            a[i] = e[i] = 0f;
        }
        a[0] = e[0] = 1f;
        a[1] = -r[1] / r[0];
        e[1] = r[0] + r[1] * a[1];
        for (int k = 1; k < order; ++k)
        {
            float lambda = 0f;
            for (int j = 0; j < k + 1; ++j)
            {
                lambda -= a[j] * r[k + 1 - j];
            }
            lambda /= e[k];

            var U = new float[k + 2];
            var V = new float[k + 2];
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
        }

        // calculate frequency characteristics
        var H = new float[N];
        for (int n = 0; n < N; ++n)
        {
            float nr = 0f, ni = 0f, dr = 0f, di = 0f;
            for (int i = 0; i < order + 1; ++i)
            {
                float re = Mathf.Cos(-2f * Mathf.PI * n * i / N);
                float im = Mathf.Sin(-2f * Mathf.PI * n * i / N);
                nr += e[order - i] * re;
                ni += e[order - i] * im;
                dr += a[order - i] * re;
                di += a[order - i] * im;
            }
            float numerator = Mathf.Sqrt(Mathf.Pow(nr, 2f) + Mathf.Pow(ni, 2f));
            float denominator = Mathf.Sqrt(Mathf.Pow(dr, 2f) + Mathf.Pow(di, 2f));
            H[n] = numerator / denominator;
        }

        // identify the first and the second formant frequency
        bool foundFirst = false;
        int f1 = 0, f2 = 0;
        for (int i = 1; i < N - 1; ++i)
        {
            if (H[i] > H[i - 1] && H[i] > H[i + 1])
            {
                if (!foundFirst)
                {
                    f1 = i;
                    foundFirst = true;
                }
                else
                {
                    f2 = i;
                    break;
                }
            }
        }

        return new FormantPair(f1 * deltaFreq, f2 * deltaFreq);
    }

    public static Vowel GetVowel(float[] input, Config config, float deltaFreq)
    {
        return GetVowel(GetFormants(input, config, deltaFreq), config);
    }

    public static Vowel GetVowel(FormantPair formant, Config config)
    {
        float diffA = FormantPair.Dist(formant, config.formantA);
        float diffI = FormantPair.Dist(formant, config.formantI);
        float diffU = FormantPair.Dist(formant, config.formantU);
        float diffE = FormantPair.Dist(formant, config.formantE);
        float diffO = FormantPair.Dist(formant, config.formantO);
        float minDiff = Mathf.Min(new float[] { diffA, diffI, diffU, diffE, diffO });

        if      (diffA == minDiff) { return Vowel.A; }
        else if (diffI == minDiff) { return Vowel.I; }
        else if (diffU == minDiff) { return Vowel.U; }
        else if (diffE == minDiff) { return Vowel.E; }
        else if (diffO == minDiff) { return Vowel.O; }

        return Vowel.None;
    }
}

}
