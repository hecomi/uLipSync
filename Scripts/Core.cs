using Unity.Mathematics;

namespace uLipSync
{

public static class Core
{
    public static float GetMaxValue(float[] input)
    {
        float max = 0f;
        for (int i = 0; i < input.Length; ++i)
        {
            max = math.max(max, math.abs(input[i]));
        }
        return max;
    }

    public static float GetVolume(float[] input)
    {
        float refAmp = 0.1f;
        float maxAmp = GetMaxValue(input);
        return math.max(20f * math.log10(maxAmp / refAmp), -160f);
    }

    public static float[] CalcLpcSpectralEnvelope(float[] input, int startIndex, Config config)
    {
        int len = input.Length;
        var data = new float[len];
        {
            int n = len - startIndex;
            System.Array.Copy(input, startIndex, data, 0, n);
            if (startIndex != 0)
            {
                System.Array.Copy(input, 0, data, n, len - n);
            }
        }

        int N = data.Length;
        int order = config.lpcOrder;

        // multiply hamming window function
        for (int i = 1; i < N - 1; ++i)
        {
            data[i] *= 0.54f - 0.46f * math.cos(2f * math.PI * i / (N - 1));
        }
        data[0] = data[N - 1] = 0f;

        // normalize
        float max = 0f, min = 0f;
        for (int i = 0; i < N; ++i)
        {
            if (data[i] > max) max = data[i];
            if (data[i] < min) min = data[i];
        }
        max = math.abs(max);
        min = math.abs(min);
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
                float re = math.cos(-2f * math.PI * n * i / N);
                float im = math.sin(-2f * math.PI * n * i / N);
                nr += e[order - i] * re;
                ni += e[order - i] * im;
                dr += a[order - i] * re;
                di += a[order - i] * im;
            }
            float numerator = math.sqrt(math.pow(nr, 2f) + math.pow(ni, 2f));
            float denominator = math.sqrt(math.pow(dr, 2f) + math.pow(di, 2f));
            H[n] = numerator / denominator;
        }

        return H;
    }

    public static FormantPair GetFormants(float[] input, int startIndex, Config config, float deltaFreq)
    {
        var H = CalcLpcSpectralEnvelope(input, startIndex, config);
        return GetFormants(H, deltaFreq);
    }

    public static FormantPair GetFormants(float[] H, float deltaFreq)
    {
        int N = H.Length;
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

        return formant;
    }

    public static Vowel GetVowel(float[] input, int startIndex, Config config, float deltaFreq)
    {
        return GetVowel(GetFormants(input, startIndex, config, deltaFreq), config);
    }

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
