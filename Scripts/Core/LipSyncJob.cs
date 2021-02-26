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
        public int index;
        public float volume;
        public float distance;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int outputSampleRate;
    [ReadOnly] public int targetSampleRate;
    [ReadOnly] public int melFilterBankChannels;
    [ReadOnly] public float volumeThresh;
    public NativeArray<float> mfcc;
    public NativeArray<float> phonemes;
    public NativeArray<Result> result;

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(input);
        if (volume < volumeThresh)
        {
            var res1 = result[0];
            res1.index = -1;
            res1.volume = volume;
            res1.distance = float.MaxValue;
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
        Algorithm.MelFilterBank(spectrum, out melSpectrum, targetSampleRate, melFilterBankChannels);

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
        GetVowel(ref res.index, ref res.distance);
        result[0] = res;

        melCepstrum.Dispose();
        melSpectrum.Dispose();
        spectrum.Dispose();
        data.Dispose();
        buffer.Dispose();
    }

    void GetVowel(ref int index, ref float minDistance)
    {
        minDistance = float.MaxValue;
        int n = phonemes.Length / 12;
        for (int i = 0; i < n; ++i)
        {
            var distance = CalcTotalDistance(i);
            if (distance < minDistance)
            {
                index = i;
                minDistance = distance;
            }
        }
    }

    float CalcTotalDistance(int index)
    {
        var distance = 0f;
        int offset = index * 12;
        for (int i = 0; i < mfcc.Length; ++i)
        {
            distance += math.abs(mfcc[i] - phonemes[i + offset]);
        }
        return distance;
    }
}

}
