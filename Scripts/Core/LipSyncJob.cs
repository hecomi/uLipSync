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
    [ReadOnly] public int sampleRate;
    [ReadOnly] public float volumeThresh;
    public NativeArray<float> mfcc;
    public NativeArray<float2> a;
    public NativeArray<float2> i;
    public NativeArray<float2> u;
    public NativeArray<float2> e;
    public NativeArray<float2> o;
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

        // copy input ring buffer to a temporary array
        var data = new NativeArray<float>(input.Length, Allocator.Temp);
        Algorithm.CopyRingBuffer(input, data, startIndex);

        // multiply window function
        Algorithm.HammingWindow(data);

        // FFT
        var spectrum = new NativeArray<float>(input.Length, Allocator.Temp);
        Algorithm.Fft(data, spectrum);

        // Mel-Filter Bank
        int melDiv = 20;
        var melSpectrum = new NativeArray<float>(melDiv, Allocator.Temp);
        Algorithm.MelFilterBankLog10(melSpectrum, spectrum, sampleRate, melDiv);

        // DCT
        var melCepstrum = new NativeArray<float>(melDiv, Allocator.Temp);
        Algorithm.DCT(melCepstrum, melSpectrum);

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
    }

    float CalcTotalDistance(NativeArray<float2> averageAndVariance)
    {
        if (averageAndVariance.Length != mfcc.Length) return float.MaxValue;

        var distance = 0f;
        for (int i = 0; i < mfcc.Length; ++i)
        {
            float2 val = averageAndVariance[i];
            distance += math.abs(mfcc[i] - val.x);// / val.y * math.abs(val.x);
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
}

}
