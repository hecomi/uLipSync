using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public struct LipSyncJob : IJob
{
    public struct Result
    {
        public Vowel vowel;
        public float volume;
        public float distance;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int outputSampleRate;
    [ReadOnly] public int targetSampleRate;
    [ReadOnly] public float volumeThresh;
    public NativeArray<float> mfcc;
    public NativeArray<float> a;
    public NativeArray<float> i;
    public NativeArray<float> u;
    public NativeArray<float> e;
    public NativeArray<float> o;
    public NativeArray<Result> result;

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(input);
        if (volume < volumeThresh)
        {
            var res1 = result[0];
            res1.volume = volume;
            result[0] = res1;
            return;
        }

        // Copy input ring buffer to a temporary array
        NativeArray<float> buffer;
        Algorithm.CopyRingBuffer(input, out buffer, startIndex);

        // LPF
        int cutoff = targetSampleRate / 2;
        int range = targetSampleRate / 4;
        Algorithm.LowPassFilter(ref buffer, outputSampleRate, cutoff, range);

        // Down sample
        NativeArray<float> data;
        Algorithm.DownSample(buffer, out data, outputSampleRate, targetSampleRate);

        // Pre-emphasis
        Algorithm.PreEmphasis(ref data, 0.97f);

        // Multiply window function
        Algorithm.HammingWindow(ref data);

        // FFT
        NativeArray<float> spectrum;
        Algorithm.FFT(data, out spectrum);

        // Mel-Filter Bank
        NativeArray<float> melSpectrum;
        Algorithm.MelFilterBank(spectrum, out melSpectrum, targetSampleRate, 32);

        // Log
        for (int i = 0; i < melSpectrum.Length; ++i)
        {
            melSpectrum[i] = math.log10(melSpectrum[i]);
        }

        // DCT
        NativeArray<float> melCepstrum;
        Algorithm.DCT(melSpectrum, out melCepstrum);

        // MFCC
        for (int i = 1; i < 13; ++i)
        {
            mfcc[i - 1] = melCepstrum[i];
        }

        // Result
        var res = new Result();
        res.volume = volume;
        GetVowel(ref res.vowel, ref res.distance);
        result[0] = res;

        melCepstrum.Dispose();
        melSpectrum.Dispose();
        spectrum.Dispose();
        data.Dispose();
        buffer.Dispose();
    }

    void GetVowel(ref Vowel vowel, ref float minDistance)
    {
        minDistance = float.MaxValue;
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var distance = CalcTotalDistance((Vowel)i);
            if (distance < minDistance)
            {
                vowel = (Vowel)i;
                minDistance = distance;
            }
        }
    }

    float CalcTotalDistance(NativeArray<float> average)
    {
        if (average.Length != mfcc.Length) return float.MaxValue;

        var distance = 0f;
        for (int i = 0; i < mfcc.Length; ++i)
        {
            distance += math.abs(mfcc[i] - average[i]);
        }
        return distance;
    }

    float CalcTotalDistance(Vowel vowel)
    {
        switch (vowel)
        {
            case Vowel.A: return CalcTotalDistance(a);
            case Vowel.I: return CalcTotalDistance(i);
            case Vowel.U: return CalcTotalDistance(u);
            case Vowel.E: return CalcTotalDistance(e);
            case Vowel.O: return CalcTotalDistance(o);
            default: return -1f;
        }
    }
}

}
