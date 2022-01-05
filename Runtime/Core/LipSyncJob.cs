using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public struct LipSyncJob : IJob
{
    public struct Info
    {
        public float volume;
        public int mainVowelIndex;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int outputSampleRate;
    [ReadOnly] public int targetSampleRate;
    [ReadOnly] public int melFilterBankChannels;
    public NativeArray<float> mfcc;
    public NativeArray<float> phonemes;
    public NativeArray<float> distances;
    public NativeArray<Info> info;

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(input);

        NativeArray<float> buffer;
        Algorithm.CopyRingBuffer(input, out buffer, startIndex);

        int cutoff = targetSampleRate / 2;
        int range = targetSampleRate / 4;
        Algorithm.LowPassFilter(ref buffer, outputSampleRate, cutoff, range);

        NativeArray<float> data;

        Algorithm.DownSample(buffer, out data, outputSampleRate, targetSampleRate);

        Algorithm.PreEmphasis(ref data, 0.97f);

        Algorithm.HammingWindow(ref data);

        NativeArray<float> spectrum;
        Algorithm.FFT(data, out spectrum);

        NativeArray<float> melSpectrum;
        Algorithm.MelFilterBank(spectrum, out melSpectrum, targetSampleRate, melFilterBankChannels);

        for (int i = 0; i < melSpectrum.Length; ++i)
        {
            melSpectrum[i] = math.log10(melSpectrum[i]);
        }

        NativeArray<float> melCepstrum;
        Algorithm.DCT(melSpectrum, out melCepstrum);

        for (int i = 1; i <= 12; ++i)
        {
            mfcc[i - 1] = melCepstrum[i];
        }

        for (int i = 0; i < distances.Length; ++i)
        {
            distances[i] = CalcTotalDistance(i);
        }

        info[0] = new Info()
        {
            volume = volume,
            mainVowelIndex = GetVowel(),
        };

        melCepstrum.Dispose();
        melSpectrum.Dispose();
        spectrum.Dispose();
        data.Dispose();
        buffer.Dispose();
    }

    int GetVowel()
    {
        int index = -1;
        float minDistance = float.MaxValue;
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
        return index;
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
